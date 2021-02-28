using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Peep.Core.API.Providers;
using Peep.Core.Infrastructure.Data;
using Peep.Crawler.Options;
using Peep.Data;
using Peep.Exceptions;
using Peep.Filtering;
using Peep.Queueing;
using Serilog;
using Serilog.Context;

namespace Peep.Crawler
{
    public class Worker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly ICrawler _crawler;
        private readonly CrawlConfigOptions _crawlOptions;
        private readonly ICrawlFilter _filter;
        private readonly ICrawlQueue _queue;
        private readonly IJobQueue _jobQueue;
        private readonly ICrawlDataSink<ExtractedData> _dataSink;
        private readonly ICrawlDataSink<CrawlError> _errorSink;
        private readonly ICrawlCancellationTokenProvider _crawlCancellationTokenProvider;

        public Worker(ILogger logger,
            ICrawler crawler,
            CrawlConfigOptions crawlOptions,
            ICrawlFilter filter,
            ICrawlQueue queue,
            IJobQueue jobQueue,
            ICrawlDataSink<ExtractedData> dataSink,
            ICrawlDataSink<CrawlError> errorSink,
            ICrawlCancellationTokenProvider crawlCancellationTokenProvider)
        {
            _logger = logger;
            _crawler = crawler;
            _crawlOptions = crawlOptions;
            _filter = filter;
            _queue = queue;
            _jobQueue = jobQueue;
            _crawlCancellationTokenProvider = crawlCancellationTokenProvider;
            _dataSink = dataSink;
            _errorSink = errorSink;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Information("Waiting for jobs");
            while (!stoppingToken.IsCancellationRequested)
            {
                // if crawl found, run the job
                while (_jobQueue.TryDequeue(out var job)) {
                    using(LogContext.PushProperty("JobId", job.Id))
                    {
                        _logger.Information("Running Job");
                        var cancellationTokenSource = 
                            CancellationTokenSource.CreateLinkedTokenSource(
                                stoppingToken, 
                                _crawlCancellationTokenProvider.GetToken(job.Id));

                        await RunJob(job, cancellationTokenSource.Token);
                    }
                }

                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task RunJob(IdentifiableCrawlJob job, CancellationToken cancellationToken)
        {
            try
            {
                var channelReader = _crawler.Crawl(
                    job,
                    _crawlOptions.ProgressUpdateDataCount,
                    _filter,
                    _queue,
                    cancellationToken);

                try
                {
                    // async iterate over channel's results
                    // update the running jobs running totals of the crawl result
                    await foreach (var result in channelReader.ReadAllAsync(cancellationToken))
                    {
                        _logger.Information("Pushing data ({Count} item(s))", result.Data.Count);
                        // send data message back to manager
                        await _dataSink.Push(job.Id, result.Data);
                    }
                }
                catch (Exception e) when (e is TaskCanceledException || e is OperationCanceledException) // cancellation token for channel reader causes this
                {
                    // occurs when cancellation occurs, so we can ignore and treat as normal
                }

                // send message to say we're complete
            }
            catch (Exception e)
            {
                // push the data we have here
                if (e is CrawlerRunException crawlerRunException)
                {
                    await _dataSink.Push(job.Id, crawlerRunException.CrawlProgress.Data);
                }
                
                // send message to say we're complete but error occurred
                await _errorSink.Push(job.Id, new CrawlError {Exception = e});
                
                _logger.Error(e, "Error occurred during crawl");
            }
            finally
            {
                _logger.Information("Crawl finished");
            }
        }
    }
}

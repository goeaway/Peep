using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Peep.Core.API.Providers;
using Peep.Core.Infrastructure.Data;
using Peep.Crawler.Options;
using Peep.Exceptions;
using Peep.Filtering;
using Peep.Queueing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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
        private readonly ICrawlDataSink _dataSink;
        private readonly ICrawlCancellationTokenProvider _crawlCancellationTokenProvider;

        public Worker(ILogger logger,
            ICrawler crawler,
            CrawlConfigOptions crawlOptions,
            ICrawlFilter filter,
            ICrawlQueue queue,
            IJobQueue jobQueue,
            ICrawlDataSink dataSink,
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
            catch (CrawlerRunException e)
            {
                // send message to say we're complete but error occurred, provide the data in the exception
                await _dataSink.Push(job.Id, e.CrawlProgress.Data);
                _logger.Error(e, "Error occurred");
            }
            catch (Exception e)
            {
                // send message to say we're complete but error occurred
                _logger.Error(e, "Unknown error occurred during crawl");
            }
            finally
            {
                _logger.Information("Crawl finished");
            }
        }
    }
}

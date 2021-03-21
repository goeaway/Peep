using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Peep.Core;
using Peep.Core.API;
using Peep.Core.API.Providers;
using Peep.Core.Exceptions;
using Peep.Core.Filtering;
using Peep.Core.Infrastructure.Messages;
using Peep.Core.Queueing;
using Peep.Crawler.Application.Options;
using Peep.Filtering;
using Peep.Queueing;
using Serilog;
using Serilog.Context;

namespace Peep.Crawler.Application.Requests.Commands.RunCrawl
{
    public class RunCrawlHandler : IRequestHandler<RunCrawlRequest, Either<Unit, HttpErrorResponse>>
    {
        private readonly ILogger _logger;
        private readonly ICrawlCancellationTokenProvider _crawlCancellationTokenProvider;
        private readonly ICrawler _crawler;
        private readonly ICrawlFilter _filter;
        private readonly ICrawlQueue _queue;
        private readonly CrawlConfigOptions _crawlConfigOptions;
        private readonly IPublishEndpoint _publishEndpoint;
        
        public RunCrawlHandler(
            ILogger logger, 
            ICrawlCancellationTokenProvider crawlCancellationTokenProvider, 
            ICrawler crawler, 
            ICrawlFilter filter, 
            ICrawlQueue queue, 
            CrawlConfigOptions crawlConfigOptions, 
            IPublishEndpoint publishEndpoint)
        {
            _logger = logger;
            _crawlCancellationTokenProvider = crawlCancellationTokenProvider;
            _crawler = crawler;
            _filter = filter;
            _queue = queue;
            _crawlConfigOptions = crawlConfigOptions;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<Either<Unit, HttpErrorResponse>> Handle(RunCrawlRequest request, CancellationToken cancellationToken)
        {
            using(LogContext.PushProperty("JobId", request.Job.Id))
            {
                _logger.Information("Running Job");
                var cancellationTokenSource = 
                    CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken, 
                        _crawlCancellationTokenProvider.GetToken(request.Job.Id));

                await RunJob(request.Job, cancellationTokenSource.Token);
            }
            
            return Unit.Value;
        }
        
        private async Task RunJob(IdentifiableCrawlJob job, CancellationToken cancellationToken)
        {
            try
            {
                var channelReader = _crawler.Crawl(
                    job,
                    _crawlConfigOptions.ProgressUpdateDataCount,
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
                        await _publishEndpoint.Publish(new CrawlDataPushed
                        {
                            JobId = job.Id,
                            Data = result.Data
                        }, cancellationToken);
                    }
                }
                catch (Exception e) when (e is TaskCanceledException || e is OperationCanceledException) // cancellation token for channel reader causes this
                {
                    // occurs when cancellation occurs, so we can ignore and treat as normal
                }
            }
            catch (Exception e)
            {
                // push the data we have here
                if (e is CrawlerRunException crawlerRunException)
                {
                    // publish message for data
                    await _publishEndpoint.Publish(new CrawlDataPushed
                    {
                        JobId = job.Id,
                        Data = crawlerRunException.CrawlProgress.Data
                    }, cancellationToken);
                }

                await _publishEndpoint
                    .Publish(new CrawlErrorPushed
                    {
                        Message = e.Message,
                        StackTrace = e.StackTrace,
                        Source = Environment.MachineName,
                        JobId = job.Id
                    }, cancellationToken);
                
                _logger.Error(e, "Error occurred during crawl");
            }
            finally
            {
                _logger.Information("Crawl finished");
            }
        }
    }
}

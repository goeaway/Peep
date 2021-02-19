using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Peep.Core.API.Providers;
using Peep.Crawler.Options;
using Peep.Exceptions;
using Peep.Filtering;
using Peep.Queueing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;

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
        private readonly ICrawlCancellationTokenProvider _crawlCancellationTokenProvider;

        public Worker(ILogger logger,
            ICrawler crawler,
            CrawlConfigOptions crawlOptions,
            ICrawlFilter filter,
            ICrawlQueue queue,
            IJobQueue jobQueue,
            ICrawlCancellationTokenProvider crawlCancellationTokenProvider)
        {
            _logger = logger;
            _crawler = crawler;
            _crawlOptions = crawlOptions;
            _filter = filter;
            _queue = queue;
            _jobQueue = jobQueue;
            _crawlCancellationTokenProvider = crawlCancellationTokenProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // if crawl found, run the job
                while (_jobQueue.TryDequeue(out var job)) {
                    var cancellationTokenSource = 
                        CancellationTokenSource.CreateLinkedTokenSource(
                            stoppingToken, 
                            _crawlCancellationTokenProvider.GetToken(job.Id));

                    await RunJob(job, cancellationTokenSource.Token);
                }

                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task RunJob(IdentifiableCrawlJob job, CancellationToken cancellationToken)
        {
            try
            {
                //var channelReader = _crawler.Crawl(
                //    job,
                //    TimeSpan.FromMilliseconds(_crawlOptions.ProgressUpdateMilliseconds),
                //    _filter,
                //    _queue,
                //    cancellationToken);

                //try
                //{
                //    // async iterate over channel's results
                //    // update the running jobs running totals of the crawl result
                //    await foreach (var result in channelReader.ReadAllAsync(cancellationToken))
                //    {
                //        // send data message back to manager
                //    }
                //}
                //catch (Exception e) when (e is TaskCanceledException || e is OperationCanceledException) // cancellation token for channel reader causes this
                //{
                //    // occurs when cancellation occurs, so we can ignore and treat as normal
                //}

                // send message to say we're complete
            }
            catch (CrawlerRunException e)
            {
                // send message to say we're complete but error occurred, provide the data in the exception
            }
            catch (Exception e)
            {
                // send message to say we're complete but error occurred
            }
        }
    }
}

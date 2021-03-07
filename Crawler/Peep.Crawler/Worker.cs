using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Hosting;
using Peep.Core.Infrastructure.Messages;
using Peep.Crawler.Application.Requests.Commands.RunCrawl;
using Peep.Crawler.Application.Services;
using Serilog;

namespace Peep.Crawler
{
    public class Worker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IJobQueue _jobQueue;
        private readonly IMediator _mediator;
        private readonly IPublishEndpoint _publishEndpoint;
        
        public Worker(
            ILogger logger,
            IJobQueue jobQueue,
            IMediator mediator, 
            IPublishEndpoint publishEndpoint)
        {
            _logger = logger;
            _jobQueue = jobQueue;
            _mediator = mediator;
            _publishEndpoint = publishEndpoint;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Information("Waiting for jobs");
            while (!stoppingToken.IsCancellationRequested)
            {
                // if crawl found, run the job
                while (_jobQueue.TryDequeue(out var job))
                {
                    // send message to API that this crawler has started this job
                    await _publishEndpoint
                        .Publish(
                            new CrawlerStarted
                            {
                                CrawlerId = Environment.MachineName, 
                                JobId = job.Id
                            },
                            stoppingToken
                        );
                    
                    await _mediator.Send(new RunCrawlRequest { Job = job }, stoppingToken);
                    
                    // send a message saying we are done
                    await _publishEndpoint
                        .Publish(
                            new CrawlerFinished
                            {
                                CrawlerId = Environment.MachineName, 
                                JobId = job.Id
                            },
                            stoppingToken
                        );
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}

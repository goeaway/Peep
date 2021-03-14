using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Hosting;
using Peep.Core.API.Options;
using Peep.Core.Infrastructure.Messages;
using Peep.Crawler.Application.Providers;
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
        private readonly ICrawlerIdProvider _crawlerIdProvider;
        private readonly ISendEndpointProvider _sendEndpointProvider;
        private readonly MonitoringOptions _monitoringOptions;

        public Worker(
            ILogger logger,
            IJobQueue jobQueue,
            IMediator mediator, 
            ICrawlerIdProvider crawlerIdProvider,
            ISendEndpointProvider sendEndpointProvider, 
            MonitoringOptions monitoringOptions)
        {
            _logger = logger;
            _jobQueue = jobQueue;
            _mediator = mediator;
            _crawlerIdProvider = crawlerIdProvider;
            _sendEndpointProvider = sendEndpointProvider;
            _monitoringOptions = monitoringOptions;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await SendUp(stoppingToken);

            stoppingToken.Register(async () => { await SendDown(); });
            
            while (!stoppingToken.IsCancellationRequested)
            {
                // if crawl found, run the job
                while (_jobQueue.TryDequeue(out var job))
                {
                    await SendJoined(stoppingToken, job);

                    var runTask = _mediator.Send(new RunCrawlRequest { Job = job }, stoppingToken);

                    while (!runTask.IsCompleted)
                    {
                        await Task.Delay(
                            TimeSpan.FromSeconds(_monitoringOptions.TickSeconds), 
                            stoppingToken);

                        await SendHeartbeat(stoppingToken);
                    }
                    
                    await SendLeft(stoppingToken, job);
                }

                await SendHeartbeat(stoppingToken);

                await Task.Delay(
                    TimeSpan.FromSeconds(_monitoringOptions.TickSeconds), 
                    stoppingToken);
            }
        }

        private async Task SendUp(CancellationToken stoppingToken)
        {
            _logger.Information("Sending up message");
            // send message saying I'm Here
            var upEndpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri("queue:crawler-up"));
            await upEndpoint
                .Send(new CrawlerUp
                {
                    CrawlerId = _crawlerIdProvider.GetCrawlerId()
                }, stoppingToken);
        }

        private async Task SendJoined(CancellationToken stoppingToken, IdentifiableCrawlJob job)
        {
            _logger.Information("Sending joined message");
            // send message to API that this crawler has started this job
            var joinedEndpoint = await _sendEndpointProvider
                .GetSendEndpoint(new Uri("queue:crawler-joined"));
            await joinedEndpoint
                .Send(
                    new CrawlerJoined
                    {
                        CrawlerId = _crawlerIdProvider.GetCrawlerId(),
                        JobId = job.Id
                    },
                    stoppingToken
                );
        }
        
        private async Task SendHeartbeat(CancellationToken cancellationToken)
        {
            var heartbeatEndpoint =
                await _sendEndpointProvider.GetSendEndpoint(new Uri("queue:crawler-heartbeat"));
                            
            await heartbeatEndpoint
                .Send(new CrawlerHeartbeat
                {
                    CrawlerId = _crawlerIdProvider.GetCrawlerId()
                }, cancellationToken);
        }
        
        private async Task SendLeft(CancellationToken stoppingToken, IdentifiableCrawlJob job)
        {
            _logger.Information("Sending left message");
            // send a message saying we are done
            var leftEndpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri("queue:crawler-left"));
            await leftEndpoint
                .Send(
                    new CrawlerLeft
                    {
                        CrawlerId = _crawlerIdProvider.GetCrawlerId(),
                        JobId = job.Id
                    },
                    stoppingToken
                );
        }
        
        private async Task SendDown()
        {
            _logger.Information("Sending down message");
            var downEndpoint = await _sendEndpointProvider
                .GetSendEndpoint(new Uri("queue:crawler-down"));
            // send message saying I'm Not working anymore
            await downEndpoint
                .Send(new CrawlerDown
                {
                    CrawlerId = _crawlerIdProvider.GetCrawlerId()
                }, CancellationToken.None);
        }
    }
}

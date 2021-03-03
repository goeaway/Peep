using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Hosting;
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

        public Worker(
            ILogger logger,
            IJobQueue jobQueue,
            IMediator mediator)
        {
            _logger = logger;
            _jobQueue = jobQueue;
            _mediator = mediator;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Information("Waiting for jobs");
            while (!stoppingToken.IsCancellationRequested)
            {
                // if crawl found, run the job
                while (_jobQueue.TryDequeue(out var job))
                {
                    await _mediator.Send(new RunCrawlRequest { Job = job }, stoppingToken);
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}

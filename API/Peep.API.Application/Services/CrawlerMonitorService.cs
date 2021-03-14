using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Hosting;
using Paramore.Brighter.Logging;
using Peep.API.Application.Requests.Commands.MonitorCrawlers;
using Peep.API.Persistence;
using Serilog;

namespace Peep.API.Application.Services
{
    public class CrawlerMonitorService : BackgroundService
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        
        public CrawlerMonitorService(IMediator mediator, ILogger logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Information("Starting crawler monitor");
            while (!stoppingToken.IsCancellationRequested)
            {
                await _mediator.Send(new MonitorCrawlersRequest(), stoppingToken);
                
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
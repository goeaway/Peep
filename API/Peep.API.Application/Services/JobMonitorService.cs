using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Hosting;
using Peep.API.Application.Requests.Commands.MonitorJobs;
using Serilog;

namespace Peep.API.Application.Services
{
    public class JobMonitorService : BackgroundService
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public JobMonitorService(IMediator mediator, ILogger logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Information("Starting job monitor service");
            while (!stoppingToken.IsCancellationRequested)
            {
                await _mediator.Send(new MonitorJobsRequest(), stoppingToken);
                
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
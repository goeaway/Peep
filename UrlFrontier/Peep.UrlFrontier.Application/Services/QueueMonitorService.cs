using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Hosting;
using Peep.UrlFrontier.Application.Commands.Monitor;

namespace Peep.UrlFrontier.Application.Services
{
    public class QueueMonitorService : BackgroundService
    {
        private readonly IMediator _mediator;

        public QueueMonitorService(IMediator mediator)
        {
            _mediator = mediator;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!stoppingToken.IsCancellationRequested)
            {
                await _mediator.Send(new MonitorRequest(), stoppingToken);
                
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
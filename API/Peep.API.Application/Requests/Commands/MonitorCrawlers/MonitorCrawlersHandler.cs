using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Peep.API.Persistence;
using Peep.Core.API.Options;
using Peep.Core.API.Providers;
using System.Linq;
using Serilog;

namespace Peep.API.Application.Requests.Commands.MonitorCrawlers
{
    public class MonitorCrawlersHandler : IRequestHandler<MonitorCrawlersRequest>
    {
        private readonly PeepApiContext _context;
        private readonly INowProvider _nowProvider;
        private readonly MonitoringOptions _monitoringOptions;
        private readonly ILogger _logger;
        
        public MonitorCrawlersHandler(
            PeepApiContext context, 
            INowProvider nowProvider, 
            MonitoringOptions monitoringOptions, 
            ILogger logger)
        {
            _context = context;
            _nowProvider = nowProvider;
            _logger = logger;
            _monitoringOptions = monitoringOptions;
        }

        public async Task<Unit> Handle(MonitorCrawlersRequest request, CancellationToken cancellationToken)
        {
            var unresponsive = _context
                .JobCrawlers
                .Where(
                    jc => jc
                        .LastHeartbeat < _nowProvider.Now - TimeSpan
                        .FromSeconds(
                            _monitoringOptions.TickSeconds * _monitoringOptions.MaxUnresponsiveTicks));

            if (!unresponsive.Any())
            {
                return Unit.Value;
            }

            _logger.Warning("Removing {Count} unresponsive crawler(s)", unresponsive.Count());
            _context.JobCrawlers.RemoveRange(unresponsive);
            await _context.SaveChangesAsync(cancellationToken);
            
            return Unit.Value;
        }
    }
}
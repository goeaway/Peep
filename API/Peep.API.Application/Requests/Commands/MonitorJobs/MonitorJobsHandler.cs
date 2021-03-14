using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Peep.API.Models.Entities;
using Peep.API.Models.Enums;
using Peep.API.Persistence;
using Peep.Core.API.Options;
using Peep.Core.API.Providers;
using Serilog;

namespace Peep.API.Application.Requests.Commands.MonitorJobs
{
    public class MonitorJobsHandler : IRequestHandler<MonitorJobsRequest, Unit>
    {
        private readonly PeepApiContext _context;
        private readonly INowProvider _nowProvider;
        private readonly MonitoringOptions _monitoringOptions;
        private readonly ILogger _logger;
        
        public MonitorJobsHandler(PeepApiContext context, INowProvider nowProvider, MonitoringOptions monitoringOptions, ILogger logger)
        {
            _context = context;
            _nowProvider = nowProvider;
            _monitoringOptions = monitoringOptions;
            _logger = logger;
        }

        public async Task<Unit> Handle(MonitorJobsRequest request, CancellationToken cancellationToken)
        {
            var unresponsive = _context
                .Jobs
                .Where(
                    jc => 
                        jc.LastHeartbeat < _nowProvider.Now - TimeSpan.FromSeconds(_monitoringOptions.TickSeconds * _monitoringOptions.MaxUnresponsiveTicks)
                        && jc.State == JobState.Running);

            if (!unresponsive.Any())
            {
                return Unit.Value;
            }

            _logger.Warning("Stopping {Count} unresponsive jobs(s)", unresponsive.Count());

            foreach (var job in unresponsive)
            {
                job.State = JobState.Errored;
                job.JobErrors.Add(new JobError
                {
                    Message = $"Job was unresponsive for {_monitoringOptions.MaxUnresponsiveTicks} ticks"
                });
            }
            
            await _context.SaveChangesAsync(cancellationToken);
            
            return Unit.Value;
        }
    }
}
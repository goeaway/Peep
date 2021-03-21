using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MediatR;
using Newtonsoft.Json;
using Peep.API.Application.Requests.Commands.RunCrawl;
using Peep.API.Models.Entities;
using Peep.API.Models.Enums;
using Peep.API.Persistence;
using Peep.Core;
using Peep.Core.API.Providers;
using Serilog;

namespace Peep.API.Application.Services
{
    public class CrawlRunnerService : BackgroundService
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly PeepApiContext _context;
        private readonly INowProvider _nowProvider;

        public CrawlRunnerService(
            ILogger logger,
            IMediator mediator,
            PeepApiContext context, 
            INowProvider nowProvider)
        {
            _logger = logger;
            _mediator = mediator;
            _context = context;
            _nowProvider = nowProvider;
        }
        
        private bool TryGetJob(out Job job)
        {
            if (_context.Jobs.Any(j => j.State == JobState.Queued)) 
            {
                // get the oldest queued job
                job = _context.Jobs
                    .OrderBy(qj => qj.DateQueued)
                    .First(j => j.State == JobState.Queued);

                // set that it is now running
                job.State = JobState.Running;
                job.DateStarted = _nowProvider.Now;
                job.LastHeartbeat = _nowProvider.Now;
                _context.SaveChanges();
                return true;
            }

            job = null;
            return false;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Information("Waiting for jobs");

            while (!stoppingToken.IsCancellationRequested)
            {
                // check for job
                if(TryGetJob(out var job))
                {
                    await _mediator.Send(new RunCrawlRequest
                    {
                        JobId =  job.Id, 
                        JobActual = JsonConvert.DeserializeObject<StoppableCrawlJob>(job.JobJson)
                    }, stoppingToken);
                } 
                else
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MediatR;
using Newtonsoft.Json;
using Peep.API.Application.Requests.Commands.RunCrawl;
using Peep.API.Models.Entities;
using Peep.API.Persistence;
using Serilog;

namespace Peep.API.Application.Services
{
    public class CrawlRunnerService : BackgroundService
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly PeepApiContext _context;
        
        public CrawlRunnerService(
            ILogger logger,
            IMediator mediator,
            PeepApiContext context)
        {
            _logger = logger;
            _mediator = mediator;
            _context = context;
        }
        
        private bool TryGetJob(out QueuedJob queuedJob)
        {
            if (_context.QueuedJobs.Any()) 
            {
                queuedJob = _context.QueuedJobs.OrderBy(qj => qj.DateQueued).First();

                _context.QueuedJobs.Remove(queuedJob);
                _context.SaveChanges();
                return true;
            }

            queuedJob = null;
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
                    await _mediator.Send(new RunCrawlRequest { Job =  job, JobData = JsonConvert.DeserializeObject<StoppableCrawlJob>(job.JobJson) }, stoppingToken);
                } 
                else
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}

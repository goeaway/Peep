using System.Linq;
using Newtonsoft.Json;
using Peep.API.Models.Entities;
using Peep.API.Persistence;

namespace Peep.API.Application.Providers
{
    public class QueuedJobProvider : IQueuedJobProvider
    {
        private readonly PeepApiContext _context;

        public QueuedJobProvider(PeepApiContext context)
        {
            _context = context;
        }
        
        public bool TryGetJob(out QueuedJob queuedJob, out StoppableCrawlJob jobData)
        {
            if (_context.QueuedJobs.Any()) 
            {
                queuedJob = _context.QueuedJobs.OrderBy(qj => qj.DateQueued).First();

                _context.QueuedJobs.Remove(queuedJob);
                _context.SaveChanges();

                jobData = JsonConvert.DeserializeObject<StoppableCrawlJob>(queuedJob.JobJson);
                return true;
            }

            queuedJob = null;
            jobData = null;
            return false;
        }
    }
}
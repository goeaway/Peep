using Peep.API.Models.Entities;

namespace Peep.API.Application.Providers
{
    public interface IQueuedJobProvider
    {
        bool TryGetJob(out QueuedJob queuedJob, out StoppableCrawlJob jobData);
    }
}
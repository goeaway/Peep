namespace Peep.Core.Infrastructure.Messages
{
    public class CrawlQueuedMessage 
    {
        public IdentifiableCrawlJob Job { get; set; }

        public CrawlQueuedMessage(IdentifiableCrawlJob job)
        {
            Job = job;
        }
    }
}

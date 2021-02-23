namespace Peep.Core.Infrastructure.Messages
{
    public class CrawlCancelledMessage
    {
        public string CrawlId { get; set; }

        public CrawlCancelledMessage(string crawlId)
        {
            CrawlId = crawlId;
        }
    }
}

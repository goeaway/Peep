namespace Peep.Core.Infrastructure.Messages
{
    public class CrawlerJoined
    {
        public CrawlerId CrawlerId { get; set; }
        public string JobId { get; set; }
    }
}
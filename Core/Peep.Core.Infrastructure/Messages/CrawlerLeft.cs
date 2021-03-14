namespace Peep.Core.Infrastructure.Messages
{
    public class CrawlerLeft
    {
        public CrawlerId CrawlerId { get; set; }
        public string JobId { get; set; }
    }
}
namespace Peep.Core.Infrastructure.Messages
{
    public class CrawlerFinished
    {
        public string CrawlerId { get; set; }
        public string JobId { get; set; }
    }
}
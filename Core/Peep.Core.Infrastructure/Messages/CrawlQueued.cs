namespace Peep.Core.Infrastructure.Messages
{
    public interface CrawlQueued 
    {
        IdentifiableCrawlJob Job { get; set; }
    }
}

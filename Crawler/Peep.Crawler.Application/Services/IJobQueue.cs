namespace Peep.Crawler.Application.Services
{
    public interface IJobQueue
    {
        bool TryDequeue(out IdentifiableCrawlJob job);
        void Enqueue(IdentifiableCrawlJob job);
        bool TryRemove(string id);
    }
}

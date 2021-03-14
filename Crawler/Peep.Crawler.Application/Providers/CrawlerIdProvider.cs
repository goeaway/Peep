using Peep.Core.Infrastructure;

namespace Peep.Crawler.Application.Providers
{
    /// <summary>
    /// Service wide injector for pre-set crawler id
    /// </summary>
    public class CrawlerIdProvider : ICrawlerIdProvider
    {
        private readonly CrawlerId _crawlerId;

        public CrawlerIdProvider(CrawlerId crawlerId)
        {
            _crawlerId = crawlerId;
        }

        /// <summary>
        /// Returns a pre-set crawler id to be used by this service to identify itself
        /// </summary>
        /// <returns></returns>
        public CrawlerId GetCrawlerId() => _crawlerId;
    }
}
using Peep.Core.Infrastructure;

namespace Peep.Crawler.Application.Providers
{
    /// <summary>
    /// Service wide injector for pre-set crawler id
    /// </summary>
    public interface ICrawlerIdProvider
    {
        /// <summary>
        /// Returns a pre-set crawler id to be used by this service to identify itself
        /// </summary>
        /// <returns></returns>
        CrawlerId GetCrawlerId();
    }
}
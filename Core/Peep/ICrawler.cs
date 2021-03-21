using Peep.Filtering;
using Peep.Queueing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Peep.Core;
using Peep.Core.Filtering;
using Peep.Core.Queueing;

namespace Peep
{
    public interface ICrawler
    {
        /// <summary>
        /// Performs a crawl based on the options in the provided <see cref="CrawlJob"/> and periodically pushes updates to a channel.
        /// </summary>
        /// <param name="job">The job options that define the crawl</param>
        /// <param name="dataUpdateCount">An integer representing a maximum amount of data the crawler should collect before offloading the data to the channel</param>
        /// <param name="queue">An <see cref="ICrawlQueue"/> the crawler can use to retrieve new URLs to crawl</param>
        /// <param name="filter">An <see cref="ICrawlFilter"/> the crawler can add crawled URLs to and check against when deciding to crawl a URL</param>
        /// <param name="cancellationToken">A cancellation token that the crawler will check throughout the crawl</param>
        /// <returns></returns>
        ChannelReader<CrawlProgress> Crawl(
            CrawlJob job, 
            int dataUpdateCount, 
            ICrawlFilter filter,
            ICrawlQueue queue,
            CancellationToken cancellationToken);
    }
}

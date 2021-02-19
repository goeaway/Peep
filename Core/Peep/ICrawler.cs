using Peep.Filtering;
using Peep.Queueing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Peep
{
    public interface ICrawler
    {
        /// <summary>
        /// Performs a crawl based on the options in the provided <see cref="CrawlJob"/> and periodically pushes updates to a channel.
        /// </summary>
        /// <param name="job">The job options that define the crawl</param>
        /// <param name="channelUpdateTimeSpan">a time interval that represents how often the crawler should write progress to the channel</param>
        /// <param name="cancellationToken">A cancellation token that the crawler will check throughout the crawl</param>
        /// <returns></returns>
        ChannelReader<CrawlProgress> Crawl(
            CrawlJob job, 
            int dateUpdateCount, 
            ICrawlFilter filter,
            ICrawlQueue queue,
            CancellationToken cancellationToken);
    }
}

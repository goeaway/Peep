using Peep.Core;
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
        ChannelReader<CrawlResult> Crawl(CrawlJob job, TimeSpan channelUpdateTimeSpan, CancellationToken cancellationToken);
        Task<CrawlResult> Crawl(CrawlJob job, CancellationToken cancellationToken);
    }
}

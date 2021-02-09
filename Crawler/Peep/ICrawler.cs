using Peep.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Peep
{
    public interface ICrawler
    {
        Task<CrawlResult> Crawl(CrawlJob job, CancellationToken cancellationToken);
        Task<CrawlResult> Crawl(CrawlJob job,
            TimeSpan progressUpdateTime,
            Action<CrawlProgress> progressUpdate,
            CancellationToken cancellationToken);
        Task<CrawlResult> Crawl(CrawlJob job,
            TimeSpan progressUpdateTime,
            Func<CrawlProgress, Task> asyncProgressUpdate,
            CancellationToken cancellationToken);
    }
}

using Peep.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Peep.Tests.Core
{
    public class FakeCrawler : ICrawler
    {
        public Task<CrawlResult> Crawl(CrawlJob job, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<CrawlResult> Crawl(CrawlJob job, TimeSpan progressUpdateTime, Action<CrawlResult> progressUpdate, CancellationToken cancellationToken)
        {
            var countdown = new Stopwatch();
            countdown.Start();

            var crawlResult = new CrawlResult
            {
                CrawlCount = 1,
                Duration = countdown.Elapsed,
                Data = new Dictionary<Uri, IEnumerable<string>>
                {
                    { new Uri("http://localhost/"), new List<string> { "data" } }
                }
            };

            while (!cancellationToken.IsCancellationRequested)
            {
                if(countdown.Elapsed >= progressUpdateTime)
                {
                    progressUpdate?.Invoke(crawlResult);
                    countdown.Restart();
                }
            }

            return Task.FromResult(crawlResult);
        }

        public Task<CrawlResult> Crawl(CrawlJob job, TimeSpan progressUpdateTime, Func<CrawlResult, Task> asyncProgressUpdate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

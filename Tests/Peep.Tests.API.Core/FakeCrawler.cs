using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Peep.Tests.Core
{
    public class FakeCrawler : ICrawler
    {
        public Task<CrawlResult> Crawl(StoppableCrawlJob job, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ChannelReader<CrawlResult> Crawl(StoppableCrawlJob job, TimeSpan channelUpdateTimeSpan, CancellationToken cancellationToken)
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

            var channel = Channel.CreateUnbounded<CrawlResult>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

            Task.Run(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (countdown.Elapsed >= channelUpdateTimeSpan)
                    {
                        channel.Writer.TryWrite(crawlResult);
                        countdown.Restart();
                    }
                }

                channel.Writer.TryWrite(crawlResult);
                channel.Writer.Complete();
            });

            return channel.Reader;
        }

        public ChannelReader<CrawlProgress> Crawl(CrawlJob job, TimeSpan channelUpdateTimeSpan, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.Factories;
using Peep.Filtering;
using Peep.PageActions;
using Peep.Queueing;

namespace Peep.Tests.Integration
{
    [TestClass]
    [TestCategory("Integration - Crawler")]
    public class CrawlerIntegrationTests
    {
        [TestMethod]
        public async Task Can_Crawl_A_Website()
        {
            var options = new CrawlerOptions
            {
                BrowserAdapterFactory = new PuppeteerSharpBrowserAdapterFactory(2, true)
            };

            var job = new CrawlJob()
            {
                Seeds = new List<Uri>
                {
                    new Uri("https://youtube.com")
                },
                DataRegex = "<h1.*?=\"title.*?<yt-formatted-string.*?ytd-video-primary-info-renderer\">(?<data>.*?)</yt-formatted-string>",
                UriRegex = ".*?watch.*?",
                PageActions = new List<IPageAction>
                {
                    new SerialisablePageAction
                    {
                        Type = SerialisablePageActionType.Wait,
                        Value = "h1.title .ytd-video-primary-info-renderer",
                    }
                }
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(20));
            
            var crawler = new DistributedCrawler(options);

            var channelReader = await crawler.Crawl(
                job,
                1,
                new BloomFilter(1000),
                new CrawlQueue(job.Seeds),
                cancellationTokenSource.Token
            );

            var data = new Dictionary<Uri, IEnumerable<string>>();

            try
            {
                await foreach (var progress in channelReader.ReadAllAsync(cancellationTokenSource.Token))
                {
                    foreach (var (key, value) in progress.Data)
                    {
                        data.Add(key, value);
                    }
                }
            }
            catch (Exception e) when (e is TaskCanceledException || e is OperationCanceledException)
            {
                // do nothing
            }
            
            Assert.AreNotEqual(0, data.Count());
        }
    }
}
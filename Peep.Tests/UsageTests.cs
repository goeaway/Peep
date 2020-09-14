using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.Abstractions;
using Peep.StopConditions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Peep.Tests
{
    [TestClass]
    public class UsageTests
    {
        [TestMethod]
        public async Task API()
        {
            var crawler = new Crawler();
            var cancellationTokenSource = new CancellationTokenSource();

            var options = new CrawlOptions
            {
                StopConditions = new List<ICrawlStopCondition>
                {
                    new MaxDurationStopCondition(TimeSpan.FromMinutes(2)),
                    new MaxCrawlStopCondition(100)
                }
            };

            var result = await crawler.Crawl(
                new Uri("https://www.reddit.com/r/pics"), 
                options, 
                cancellationTokenSource.Token);

            Assert.AreEqual(0, result.CrawlCount);
        }
    }
}

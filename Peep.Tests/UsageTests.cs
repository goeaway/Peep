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
                UriRegex = "https://www.youtube.com/watch.*?",
                DataRegex = "<h1.*?=\"title.*?<yt-formatted-string.*?ytd-video-primary-info-renderer\">(?<data>.*?)</yt-formatted-string>",
                WaitOptions = new WaitOptions
                {
                    MillisecondsTimeout = 3000,
                    Selector = "h1.title .ytd-video-primary-info-renderer"
                },
                StopConditions = new List<ICrawlStopCondition>
                {
                    new MaxDurationStopCondition(TimeSpan.FromMinutes(10)),
                    //new MaxCrawlStopCondition(10),
                    new MaxDataStopCondition(10)
                }
            };

            var result = await crawler.Crawl(
                new Uri("https://www.youtube.com/"), 
                options, 
                cancellationTokenSource.Token);

            Assert.AreEqual(10, result.Data.Count);
        }
    }
}

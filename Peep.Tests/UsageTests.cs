using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.Core;
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

            var job = new CrawlJob
            {
                Seeds = new List<Uri>
                {
                    new Uri("https://www.youtube.com/"), 
                },
                UriRegex = "https://www.youtube.com/watch.*?",
                DataRegex = "<h1.*?=\"title.*?<yt-formatted-string.*?ytd-video-primary-info-renderer\">(?<data>.*?)</yt-formatted-string>",
                WaitOptions = new WaitOptions
                {
                    MillisecondsTimeout = 3000,
                    Selector = "h1.title .ytd-video-primary-info-renderer"
                },
                StopConditions = new List<SerialisableStopCondition>
                {
                    new SerialisableStopCondition
                    {
                        Value = TimeSpan.FromMinutes(10).TotalSeconds,
                        Type = SerialisableStopConditionType.MaxDurationSeconds
                    },
                    new SerialisableStopCondition
                    {
                        Value = 10,
                        Type = SerialisableStopConditionType.MaxDataCount
                    }
                }
            };

            var result = await crawler.Crawl(
                job, 
                cancellationTokenSource.Token);

            Assert.AreEqual(10, result.Data.Count);
        }
    }
}

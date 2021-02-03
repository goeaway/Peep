using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Peep.PageActions;
using Peep.StopConditions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Peep.Tests
{
    [TestClass]
    [TestCategory("Misc")]
    public class MiscTests
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
                PageActions = new List<SerialisablePageAction>
                {
                    new SerialisablePageAction
                    {
                        Value = "h1.title .ytd-video-primary-info-renderer",
                        Type = SerialisablePageActionType.Wait
                    }
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

            //var result = await crawler.Crawl(
            //    job, 
            //    cancellationTokenSource.Token);

            //Assert.AreEqual(10, result.Data.Count);
        }

        [TestMethod]
        public void JobFileGenerator()
        {
            const string PATH = @"C:\Users\Joe\Documents\Peep\Jobs";

            var job = new CrawlJob
            {
                Seeds = new List<Uri>
                {
                    new Uri("https://www.youtube.com/"),
                },
                UriRegex = "https://www.youtube.com/watch.*?",
                DataRegex = "<h1.*?=\"title.*?<yt-formatted-string.*?ytd-video-primary-info-renderer\">(?<data>.*?)</yt-formatted-string>",
                PageActions = new List<SerialisablePageAction>
                {
                    new SerialisablePageAction
                    {
                        Value = "h1.title .ytd-video-primary-info-renderer",
                        Type = SerialisablePageActionType.Wait
                    }
                },
                StopConditions = new List<SerialisableStopCondition>
                {
                    new SerialisableStopCondition
                    {
                        Value = TimeSpan.FromSeconds(10).TotalSeconds,
                        Type = SerialisableStopConditionType.MaxDurationSeconds
                    },
                    new SerialisableStopCondition
                    {
                        Value = 10,
                        Type = SerialisableStopConditionType.MaxDataCount
                    }
                }
            };

            File.WriteAllText(Path.Combine(PATH, Guid.NewGuid() + ".json"), JsonConvert.SerializeObject(job, Formatting.Indented));
        }
    }
}

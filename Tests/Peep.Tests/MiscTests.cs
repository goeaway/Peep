// using System;
// using System.Collections.Generic;
// using System.Threading;
// using System.Threading.Tasks;
// using Microsoft.VisualStudio.TestTools.UnitTesting;
// using Peep.BrowserAdapter;
// using Peep.Core;
// using Peep.Core.PageActions;
// using Peep.Filtering;
// using Peep.Queueing;
//
// namespace Peep.Tests
// {
//     [TestCategory("Ignore")]
//     [TestClass]
//     public class MiscTests
//     {
//         [TestMethod]
//         public async Task Playwright_Test()
//         {
//             using var adapter = new PlaywrightSharpBrowserAdapter(3);
//             
//             var crawler = new DistributedCrawler(adapter);
//
//             var job = new CrawlJob
//             {
//                 Seeds = new List<Uri>
//                 {
//                     new Uri("https://www.youtube.com/")
//                 },
//                 UriRegex = "https://www.youtube.com/watch.*?",
//                 DataRegex =
//                     "<h1.*?=\"title.*?<yt-formatted-string.*?ytd-video-primary-info-renderer\">(?<data>.*?)</yt-formatted-string>",
//                 IgnoreRobots = false,
//                 PageActions = new List<IPageAction>
//                 {
//                     new SerialisablePageAction
//                     {
//                         UriRegex = "https://www.youtube.com/watch.*?",
//                         Value = "h1.title .ytd-video-primary-info-renderer",
//                         Type = 0
//                     }
//                 }
//             };
//
//             var cancellationTokenSource = new CancellationTokenSource();
//             cancellationTokenSource.CancelAfter(30_000);
//
//             var reader = crawler.Crawl(
//                 job,
//                 3,
//                 new BloomFilter(1000),
//                 new CrawlQueue(job.Seeds),
//                 cancellationTokenSource.Token);
//
//             var data = new Dictionary<Uri, IEnumerable<string>>();
//             
//             try
//             {
//                 await foreach (var result in reader.ReadAllAsync(cancellationTokenSource.Token))
//                 {
//                     foreach (var (key, value) in result.Data)
//                     {
//                         data.Add(key, value);
//                     }
//                 }
//             }
//             catch (Exception)
//             {
//                 // ignore
//             }
//             
//             Assert.AreNotEqual(0, data.Count);
//         }
//     }
// }
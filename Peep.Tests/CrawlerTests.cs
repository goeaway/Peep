using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.BrowserAdapter;
using Peep.Factories;
using Peep.StopConditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Peep.Tests
{
    [TestClass]
    [TestCategory("Crawler")]
    public class CrawlerTests
    {
        [TestMethod]
        public void Throws_If_CrawlerOptions_Null()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new Crawler(null));
        }

        [TestMethod]
        public async Task Crawl_Throws_If_Seed_Null()
        {
            var crawler = new Crawler();
            var cancellationToken = new CancellationTokenSource().Token;
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => crawler.Crawl(null as Uri, cancellationToken));
        }

        [TestMethod]
        public async Task Crawl_Throws_If_Seeds_Null()
        {
            var crawler = new Crawler();
            var cancellationToken = new CancellationTokenSource().Token;
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(
                () => crawler.Crawl(null as IEnumerable<Uri>, cancellationToken));
        }

        [TestMethod]
        public async Task Crawl_Throws_If_Options_Null()
        {
            var URI = new Uri("http://localhost");
            var CANCELLATION_TOKEN = new CancellationTokenSource().Token;

            var crawler = new Crawler();
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(
                () => crawler.Crawl(
                    URI, 
                    null,
                    CANCELLATION_TOKEN));
        }

        [TestMethod]
        public async Task Crawl_Throws_If_Seeds_Count_Zero()
        {
            var URIs = new List<Uri>();
            var CANCELLATION_TOKEN = new CancellationTokenSource().Token;

            var crawler = new Crawler();
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => crawler.Crawl(
                    URIs,
                    CANCELLATION_TOKEN));
        }

        [TestMethod]
        public async Task Stops_If_CancellationToken_Triggered()
        {
            var URI = new Uri("http://localhost");

            var mockBrowserAdapterFactory = new Mock<IBrowserAdapterFactory>();
            var mockBrowserAdapter = new Mock<IBrowserAdapter>();
            mockBrowserAdapterFactory.Setup(mock => mock.GetBrowserAdapter())
                .ReturnsAsync(mockBrowserAdapter.Object);

            var options = new CrawlerOptions
            {
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object
            };

            var crawler = new Crawler(options);

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var result = await crawler.Crawl(URI, cancellationTokenSource.Token);

            mockBrowserAdapter.Verify(
                mock => mock.NavigateToAsync(It.IsAny<Uri>()), 
                Times.Never());
        } 

        [TestMethod]
        public async Task Stops_If_One_StopCondition_For_Crawl_Triggered()
        {
            var URI = new Uri("http://localhost");

            var mockBrowserAdapterFactory = new Mock<IBrowserAdapterFactory>();
            var mockBrowserAdapter = new Mock<IBrowserAdapter>();
            mockBrowserAdapterFactory.Setup(mock => mock.GetBrowserAdapter())
                .ReturnsAsync(mockBrowserAdapter.Object);

            var options = new CrawlerOptions
            {
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object
            };

            var crawler = new Crawler(options);

            var cancellationTokenSource = new CancellationTokenSource();

            var crawlOptions = new CrawlOptions
            {
                StopConditions = new List<ICrawlStopCondition>
                {
                    new MaxDurationStopCondition(TimeSpan.MinValue),
                    new MaxCrawlStopCondition(1000)
                }
            };

            var result = await crawler.Crawl(URI, crawlOptions, cancellationTokenSource.Token);

            mockBrowserAdapter.Verify(
                mock => mock.NavigateToAsync(It.IsAny<Uri>()),
                Times.Never());
        }

        [TestMethod]
        public async Task Returns_Crawl_Result_With_Data_CrawlCount_Duration()
        {
            var URI = new Uri("http://localhost");
            var EXTRACTED_DATA = "extracted data";

            var mockBrowserAdapterFactory = new Mock<IBrowserAdapterFactory>();
            var mockBrowserAdapter = new Mock<IBrowserAdapter>();
            mockBrowserAdapterFactory.Setup(mock => mock.GetBrowserAdapter())
                .ReturnsAsync(mockBrowserAdapter.Object);

            mockBrowserAdapter.Setup(mock => mock.NavigateToAsync(URI)).ReturnsAsync(true);
            mockBrowserAdapter.Setup(mock => mock.GetUserAgentAsync()).ReturnsAsync("user-agent");
            mockBrowserAdapter.Setup(mock => mock.GetContentAsync()).ReturnsAsync(EXTRACTED_DATA);

            var options = new CrawlerOptions
            {
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object
            };

            var crawler = new Crawler(options);

            var cancellationTokenSource = new CancellationTokenSource();

            var crawlOptions = new CrawlOptions
            {
                DataRegex = $"(?<data>{EXTRACTED_DATA})",
                StopConditions = new List<ICrawlStopCondition>
                {
                    new MaxDataStopCondition(1)
                }
            };

            var result = await crawler.Crawl(URI, crawlOptions, cancellationTokenSource.Token);

            Assert.AreEqual(1, result.CrawlCount);
            Assert.AreEqual(1, result.Data.Count);
            Assert.AreNotEqual(TimeSpan.MinValue, result.Duration);

            Assert.AreEqual(new Uri("http://localhost/"), result.Data.First().Key);
            Assert.AreEqual(EXTRACTED_DATA, result.Data.First().Value.First());
        }

        [TestMethod]
        public async Task Does_Not_Crawl_The_Same_Page_Twice()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Does_Not_Crawl_Forbidden_Page()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Does_Crawl_Forbidden_Page_If_Ignore_Robots_Option_Is_True()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Does_Not_Crawl_Page_That_Does_Not_Match_Source()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Does_Crawl_Page_If_URI_Matches_UriRegex()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Does_Not_Crawl_Page_If_URI_Matches_UriRegex_But_Host_Not_Same()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Waits_For_Selector()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Times_Out_Waiting_For_Selector_But_Continues()
        {
            Assert.Fail();
        }
    }
}

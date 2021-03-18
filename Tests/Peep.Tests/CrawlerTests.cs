using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.BrowserAdapter;
using Peep.Exceptions;
using Peep.PageActions;
using Peep.StopConditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Peep.Robots;
using Peep.Filtering;
using Peep.Queueing;

namespace Peep.Tests
{
    [TestClass]
    [TestCategory("Core - Crawler - Crawler")]
    public class CrawlerTests
    {
        [TestMethod]
        public void Throws_If_BrowserAdapter_Null()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new DistributedCrawler(null));
        }
        
        [TestMethod]
        public void Throws_If_CrawlerOptions_Null()
        {
            var browserAdapter = new Mock<IBrowserAdapter>();
            Assert.ThrowsException<ArgumentNullException>(() => new DistributedCrawler(browserAdapter.Object, null));
        }

        [TestMethod]
        public void Throws_If_CrawlerOptions_DataExtractor_Null()
        {
            var options = new CrawlerOptions
            {
                DataExtractor = null
            };
            var browserAdapter = new Mock<IBrowserAdapter>();
            Assert.ThrowsException<CrawlerOptionsException>(() => new DistributedCrawler(browserAdapter.Object, options));
        }

        [TestMethod]
        public void Throws_If_CrawlerOptions_RobotParser_Null()
        {
            var options = new CrawlerOptions
            {
                RobotParser = null
            };
            var browserAdapter = new Mock<IBrowserAdapter>();
            Assert.ThrowsException<CrawlerOptionsException>(() => new DistributedCrawler(browserAdapter.Object, options));
        }

        [TestMethod]
        public void Crawl_Throws_If_Options_Null()
        {
            var browserAdapter = new Mock<IBrowserAdapter>();
            var crawler = new DistributedCrawler(browserAdapter.Object);
            Assert.ThrowsException<ArgumentNullException>(
                () => crawler.Crawl(null, 1, new BloomFilter(100), new CrawlQueue(), default));
        }

        [TestMethod]
        public void Crawl_Throws_If_Seeds_Null()
        {
            var browserAdapter = new Mock<IBrowserAdapter>();
            var crawler = new DistributedCrawler(browserAdapter.Object);
            var job = new StoppableCrawlJob();
            Assert.ThrowsException<InvalidOperationException>(
                () => crawler.Crawl(job, 1, new BloomFilter(100), new CrawlQueue(), default));
        }

        [TestMethod]
        public void Crawl_Throws_If_Seeds_Empty()
        {
            var job = new StoppableCrawlJob
            {
                Seeds = new List<Uri>()
            };

            var browserAdapter = new Mock<IBrowserAdapter>();
            var crawler = new DistributedCrawler(browserAdapter.Object);
            Assert.ThrowsException<InvalidOperationException>(
                () => crawler.Crawl(job, 1, new BloomFilter(100), new CrawlQueue(), default));
        }

        [TestMethod]
        public void Crawl_Throws_If_Filter_Null()
        {
            var job = new StoppableCrawlJob
            {
                Seeds = new List<Uri>
                {
                    new Uri("http://localhost")
                }
            };

            var browserAdapter = new Mock<IBrowserAdapter>();
            var crawler = new DistributedCrawler(browserAdapter.Object);
            Assert.ThrowsException<ArgumentNullException>(
                () => crawler.Crawl(job, 1, null, new CrawlQueue(), default));
        }

        [TestMethod]
        public void Crawl_Throws_If_Queue_Null()
        {
            var job = new StoppableCrawlJob
            {
                Seeds = new List<Uri>
                {
                    new Uri("http://localhost")
                }
            };

            var browserAdapter = new Mock<IBrowserAdapter>();
            var crawler = new DistributedCrawler(browserAdapter.Object);
            Assert.ThrowsException<ArgumentNullException>(
                () => crawler.Crawl(job, 1, new BloomFilter(100), null, default));
        }

        [TestMethod]
        public void Crawl_Throws_If_DataCountUpdate_LessThanOne()
        {
            var job = new StoppableCrawlJob
            {
                Seeds = new List<Uri>
                {
                    new Uri("http://localhost")
                }
            };

            var browserAdapter = new Mock<IBrowserAdapter>();
            var crawler = new DistributedCrawler(browserAdapter.Object);
            Assert.ThrowsException<InvalidOperationException>(
                () => crawler.Crawl(job, 0, new BloomFilter(100), new CrawlQueue(), default));
        }

        [TestMethod]
        public void Stops_If_CancellationToken_Triggered()
        {
            var job = new StoppableCrawlJob
            {
                Seeds = new List<Uri>
                {
                    new Uri("http://localhost")
                }
            };

            var mockBrowserAdapter = new Mock<IBrowserAdapter>();
            var mockPageAdapter = new Mock<IPageAdapter>();
            
            var crawler = new DistributedCrawler(mockBrowserAdapter.Object);

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            crawler.Crawl(job, 
                1, 
                new BloomFilter(100),
                new CrawlQueue(job.Seeds),
                cancellationTokenSource.Token);

            mockPageAdapter.Verify(
                mock => mock.NavigateToAsync(It.IsAny<Uri>()),
                Times.Never());
        }

        [TestMethod]
        public async Task Does_Not_Crawl_The_Same_Page_Twice()
        {
            var uri = new Uri("http://localhost/");
            const string EXTRACTED_DATA = "<a href='/'></a><a href='/some-other-page'></a>";
            const string USER_AGENT = "user-agent";
            var cancellationTokenSource = new CancellationTokenSource();
            var job = new StoppableCrawlJob
            {
                IgnoreRobots = false,
                Seeds = new List<Uri>
                {
                    uri
                }
            };

            var mockBrowserAdapter = new Mock<IBrowserAdapter>();
            var mockPageAdapter = new Mock<IPageAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();

            mockBrowserAdapter.Setup(mock => mock.GetPageAdapters())
                .Returns(new List<IPageAdapter> {mockPageAdapter.Object});
            
            mockBrowserAdapter.Setup(mock => mock.GetUserAgentAsync()).ReturnsAsync(USER_AGENT);
            mockPageAdapter.Setup(mock => mock.NavigateToAsync(It.IsAny<Uri>())).ReturnsAsync(true);
            mockPageAdapter.Setup(mock => mock.GetContentAsync()).ReturnsAsync(EXTRACTED_DATA);

            mockRobotParser.Setup(mock => mock.UriForbidden(It.IsAny<Uri>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            var crawlerOptions = new CrawlerOptions
            {
                RobotParser = mockRobotParser.Object,
            };

            var crawler = new DistributedCrawler(mockBrowserAdapter.Object, crawlerOptions);

            var result = crawler.Crawl(
                job,
                1,
                new BloomFilter(100),
                new CrawlQueue(job.Seeds),
                cancellationTokenSource.Token);

            await Task.Delay(1000);
            cancellationTokenSource.Cancel();

            await result.WaitToReadAsync();

            mockPageAdapter.Verify(
                mock => mock.NavigateToAsync(
                    It.Is<Uri>(uri => uri.AbsoluteUri == "http://localhost/some-other-page/")),
                Times.Once());
        }

        [TestMethod]
        public async Task Does_Not_Crawl_Forbidden_Page()
        {
            const string EXTRACTED_DATA = "<a href='/forbidden'></a>";
            const string USER_AGENT = "user-agent";
            var cancellationTokenSource = new CancellationTokenSource();
            var uri = new Uri("http://localhost");
            var job = new StoppableCrawlJob
            {
                Seeds = new List<Uri>
                {
                    uri
                }
            };

            var mockBrowserAdapter = new Mock<IBrowserAdapter>();
            var mockPageAdapter = new Mock<IPageAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();

            mockBrowserAdapter.Setup(mock => mock.GetPageAdapters())
                .Returns(new List<IPageAdapter> {mockPageAdapter.Object});

            mockBrowserAdapter.Setup(mock => mock.GetUserAgentAsync()).ReturnsAsync(USER_AGENT);
            mockPageAdapter.Setup(mock => mock.NavigateToAsync(uri)).ReturnsAsync(true);
            mockPageAdapter.Setup(mock => mock.GetContentAsync()).ReturnsAsync(EXTRACTED_DATA);

            mockRobotParser.Setup(mock => mock.UriForbidden(It.IsAny<Uri>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var crawlerOptions = new CrawlerOptions
            {
                RobotParser = mockRobotParser.Object,
            };

            var crawler = new DistributedCrawler(mockBrowserAdapter.Object, crawlerOptions);

            var result = crawler.Crawl(
                job, 
                1,
                new BloomFilter(100),
                new CrawlQueue(job.Seeds),
                cancellationTokenSource.Token);

            await Task.Delay(1000);
            cancellationTokenSource.Cancel();

            await result.WaitToReadAsync();
            mockPageAdapter
                .Verify(
                    mock => mock.NavigateToAsync(It.Is<Uri>(uri => uri.AbsoluteUri == "http://localhost/forbidden/")),
                    Times.Never());
        }

        [TestMethod]
        public async Task Does_Crawl_Forbidden_Page_If_Ignore_Robots_Option_Is_True()
        {
            const string EXTRACTED_DATA = "<a href='/forbidden'></a>";
            const string USER_AGENT = "user-agent";
            var cancellationTokenSource = new CancellationTokenSource();
            var uri = new Uri("http://localhost");
            var job = new StoppableCrawlJob
            {
                IgnoreRobots = true,
                Seeds = new List<Uri>
                {
                    uri
                }
            };

            var mockBrowserAdapter = new Mock<IBrowserAdapter>();
            var mockPageAdapter = new Mock<IPageAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();

            mockBrowserAdapter.Setup(mock => mock.GetPageAdapters())
                .Returns(new List<IPageAdapter> {mockPageAdapter.Object});

            mockBrowserAdapter.Setup(mock => mock.GetUserAgentAsync()).ReturnsAsync(USER_AGENT);
            mockPageAdapter.Setup(mock => mock.NavigateToAsync(It.IsAny<Uri>())).ReturnsAsync(true);
            mockPageAdapter.Setup(mock => mock.GetContentAsync()).ReturnsAsync(EXTRACTED_DATA);

            mockRobotParser.Setup(mock => mock.UriForbidden(It.IsAny<Uri>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var crawlerOptions = new CrawlerOptions
            {
                RobotParser = mockRobotParser.Object,
            };

            var crawler = new DistributedCrawler(mockBrowserAdapter.Object, crawlerOptions);

            var result = crawler.Crawl(
                job, 
                1, 
                new BloomFilter(100), 
                new CrawlQueue(job.Seeds), 
                cancellationTokenSource.Token);

            await Task.Delay(1000);
            cancellationTokenSource.Cancel();

            await result.WaitToReadAsync();

            mockPageAdapter
                .Verify(
                    mock => mock.NavigateToAsync(It.Is<Uri>(uri => uri.AbsoluteUri == "http://localhost/forbidden/")),
                    Times.Once());
        }

        [TestMethod]
        public async Task Does_Not_Crawl_Page_That_Does_Not_Match_Source()
        {
            var uri = new Uri("http://localhost/");
            const string EXTRACTED_DATA = "<a href='//test.com/'></a><a href='/valid'></a>";
            const string USER_AGENT = "user-agent";
            var cancellationTokenSource = new CancellationTokenSource();
            var job = new StoppableCrawlJob
            {
                Seeds = new List<Uri>
                {
                    uri
                },
                StopConditions = new List<SerialisableStopCondition>
                {
                    new SerialisableStopCondition
                    {
                        Value = 2,
                        Type = SerialisableStopConditionType.MaxCrawlCount
                    }
                }
            };

            var mockBrowserAdapter = new Mock<IBrowserAdapter>();
            var mockPageAdapter = new Mock<IPageAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();

            mockBrowserAdapter.Setup(mock => mock.GetPageAdapters())
                .Returns(new List<IPageAdapter> {mockPageAdapter.Object});

            mockBrowserAdapter.Setup(mock => mock.GetUserAgentAsync()).ReturnsAsync(USER_AGENT);
            mockPageAdapter.Setup(mock => mock.NavigateToAsync(It.IsAny<Uri>())).ReturnsAsync(true);
            mockPageAdapter.Setup(mock => mock.GetContentAsync()).ReturnsAsync(EXTRACTED_DATA);

            mockRobotParser.Setup(mock => mock.UriForbidden(It.IsAny<Uri>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            var crawlerOptions = new CrawlerOptions
            {
                RobotParser = mockRobotParser.Object
            };

            var crawler = new DistributedCrawler(mockBrowserAdapter.Object, crawlerOptions);

            var result = crawler.Crawl(job, 1, new BloomFilter(100), new CrawlQueue(job.Seeds), cancellationTokenSource.Token);

            await Task.Delay(1000);
            cancellationTokenSource.Cancel();

            await result.WaitToReadAsync();
            mockPageAdapter
                .Verify(mock =>
                    mock.NavigateToAsync(It.Is<Uri>(uri => uri.AbsoluteUri == "https://test.com/")),
                    Times.Never());
        }

        [TestMethod]
        public async Task Does_Crawl_Page_If_URI_Matches_UriRegex_On_Same_Domain()
        {
            var uri = new Uri("http://localhost/an-area");
            const string EXTRACTED_DATA = "<a href='/different-area'></a>";
            const string USER_AGENT = "user-agent";
            var cancellationTokenSource = new CancellationTokenSource();
            var job = new StoppableCrawlJob
            {
                UriRegex = "/different-area.*?",
                Seeds = new List<Uri>
                {
                    uri
                },
                StopConditions = new List<SerialisableStopCondition>
                {
                    new SerialisableStopCondition
                    {
                        Value = 2,
                        Type = SerialisableStopConditionType.MaxCrawlCount
                    }
                }
            };

            var mockBrowserAdapter = new Mock<IBrowserAdapter>();
            var mockPageAdapter = new Mock<IPageAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();

            mockBrowserAdapter.Setup(mock => mock.GetPageAdapters())
                .Returns(new List<IPageAdapter> {mockPageAdapter.Object});
            
            mockBrowserAdapter.Setup(mock => mock.GetUserAgentAsync()).ReturnsAsync(USER_AGENT);
            mockPageAdapter.Setup(mock => mock.NavigateToAsync(It.IsAny<Uri>())).ReturnsAsync(true);
            mockPageAdapter.Setup(mock => mock.GetContentAsync()).ReturnsAsync(EXTRACTED_DATA);

            mockRobotParser.Setup(mock => mock.UriForbidden(It.IsAny<Uri>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            var crawlerOptions = new CrawlerOptions
            {
                RobotParser = mockRobotParser.Object
            };

            var crawler = new DistributedCrawler(mockBrowserAdapter.Object, crawlerOptions);

            var result = crawler.Crawl(job, 1, new BloomFilter(100), new CrawlQueue(job.Seeds), cancellationTokenSource.Token);

            await Task.Delay(1000);
            cancellationTokenSource.Cancel();

            await result.WaitToReadAsync();

            mockPageAdapter.Verify(
                mock => mock.NavigateToAsync(It.Is<Uri>(uri => uri.AbsoluteUri == "http://localhost/different-area/")),
                Times.Once());
        }

        [TestMethod]
        public async Task Does_Not_Crawl_Page_If_URI_Matches_UriRegex_On_Different_Domain()
        {
            var uri = new Uri("http://localhost/");
            const string EXTRACTED_DATA = "<a href='//test.com'></a>";
            const string USER_AGENT = "user-agent";
            var cancellationTokenSource = new CancellationTokenSource();
            var job = new StoppableCrawlJob
            {
                UriRegex = "//test.com*?",
                Seeds = new List<Uri>
                {
                    uri
                },
                StopConditions = new List<SerialisableStopCondition>
                {
                    new SerialisableStopCondition
                    {
                        Value = 2,
                        Type = SerialisableStopConditionType.MaxCrawlCount
                    }
                }
            };

            var mockBrowserAdapter = new Mock<IBrowserAdapter>();
            var mockPageAdapter = new Mock<IPageAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();

            mockBrowserAdapter.Setup(mock => mock.GetPageAdapters())
                .Returns(new List<IPageAdapter> {mockPageAdapter.Object});

            mockBrowserAdapter.Setup(mock => mock.GetUserAgentAsync()).ReturnsAsync(USER_AGENT);
            mockPageAdapter.Setup(mock => mock.NavigateToAsync(uri)).ReturnsAsync(true);
            mockPageAdapter.Setup(mock => mock.GetContentAsync()).ReturnsAsync(EXTRACTED_DATA);

            mockRobotParser.Setup(mock => mock.UriForbidden(It.IsAny<Uri>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            var crawlerOptions = new CrawlerOptions
            {
                RobotParser = mockRobotParser.Object,
            };

            var crawler = new DistributedCrawler(mockBrowserAdapter.Object, crawlerOptions);

            var result = crawler.Crawl(job, 1, new BloomFilter(100), new CrawlQueue(job.Seeds), cancellationTokenSource.Token);

            await Task.Delay(1000);
            cancellationTokenSource.Cancel();

            await result.WaitToReadAsync();

            mockPageAdapter.Verify(
                mock => mock.NavigateToAsync(It.Is<Uri>(uri => uri.AbsoluteUri == "https://test.com/")),
                Times.Never());
        }

        [TestMethod]
        public async Task Crawl_Performs_Page_Actions_Before_Getting_Content()
        {
            var uri = new Uri("http://localhost/");
            const string USER_AGENT = "user-agent";

            var cancellationTokenSource = new CancellationTokenSource();

            var mockPageAction = new Mock<IPageAction>();
            var mockPageActionPerformer = new Mock<IPageActionPerformer>();

            var job = new StoppableCrawlJob
            {
                Seeds = new List<Uri>
                {
                    uri
                },
                PageActions = new List<IPageAction>
                {
                    mockPageAction.Object
                },
                StopConditions = new List<SerialisableStopCondition>
                {
                    new SerialisableStopCondition
                    {
                        Value = 1,
                        Type = SerialisableStopConditionType.MaxCrawlCount
                    }
                }
            };

            var mockBrowserAdapter = new Mock<IBrowserAdapter>();
            var mockPageAdapter = new Mock<IPageAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();

            mockBrowserAdapter.Setup(mock => mock.GetPageAdapters())
                .Returns(new List<IPageAdapter> {mockPageAdapter.Object});

            mockPageAdapter.Setup(mock => mock.NavigateToAsync(uri)).ReturnsAsync(true);
            mockBrowserAdapter.Setup(mock => mock.GetUserAgentAsync()).ReturnsAsync(USER_AGENT);

            var crawlerOptions = new CrawlerOptions
            {
                RobotParser = mockRobotParser.Object,
                PageActionPerformer = mockPageActionPerformer.Object
            };

            var crawler = new DistributedCrawler(mockBrowserAdapter.Object, crawlerOptions);

            var result = crawler.Crawl(job, 1, new BloomFilter(100), new CrawlQueue(job.Seeds), cancellationTokenSource.Token);

            await Task.Delay(1000);
            cancellationTokenSource.Cancel();

            await result.WaitToReadAsync();
            mockPageActionPerformer.Verify(mock => mock.Perform(mockPageAction.Object, mockPageAdapter.Object), Times.Once());
        }

        [TestMethod]
        public async Task Crawl_Only_Performs_Page_Action_If_Page_URL_Matches_Uri_Regex()
        {
            var uri = new Uri("http://localhost/");
            const string USER_AGENT = "user-agent";
            var cancellationTokenSource = new CancellationTokenSource();

            var mockPageAction = new Mock<IPageAction>();
            mockPageAction.Setup(mock => mock.UriRegex).Returns("anything");
            var mockPageActionPerformer = new Mock<IPageActionPerformer>();

            var job = new StoppableCrawlJob
            {
                Seeds = new List<Uri>
                {
                    uri
                },
                PageActions = new List<IPageAction>
                {
                    mockPageAction.Object
                }
            };

            var mockBrowserAdapter = new Mock<IBrowserAdapter>();
            var mockPageAdapter = new Mock<IPageAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();

            mockBrowserAdapter.Setup(mock => mock.GetPageAdapters())
                .Returns(new List<IPageAdapter> {mockPageAdapter.Object});

            mockBrowserAdapter.Setup(mock => mock.GetUserAgentAsync()).ReturnsAsync(USER_AGENT);
            mockPageAdapter.Setup(mock => mock.NavigateToAsync(uri)).ReturnsAsync(true);

            var crawlerOptions = new CrawlerOptions
            {
                RobotParser = mockRobotParser.Object,
                PageActionPerformer = mockPageActionPerformer.Object
            };

            var crawler = new DistributedCrawler(mockBrowserAdapter.Object, crawlerOptions);

            var result = crawler.Crawl(job, 1, new BloomFilter(100), new CrawlQueue(job.Seeds), cancellationTokenSource.Token);

            await Task.Delay(1000);
            cancellationTokenSource.Cancel();

            await result.WaitToReadAsync();
            mockPageActionPerformer
                .Verify(mock => mock.Perform(mockPageAction.Object, mockPageAdapter.Object), Times.Never());
        }

        [TestMethod]
        public async Task Crawl_Should_Periodically_Write_Data_To_Channel()
        {
            var uri = new Uri("http://localhost/");
            const string EXTRACTED_DATA = "<a href='//test.com/'></a>data";
            const string USER_AGENT = "user-agent";
            var cancellationTokenSource = new CancellationTokenSource();
            var job = new StoppableCrawlJob
            {
                Seeds = new List<Uri>
                {
                    uri
                },
                DataRegex = "(?<data>data)"
            };

            var mockBrowserAdapter = new Mock<IBrowserAdapter>();
            var mockPageAdapter = new Mock<IPageAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();
            var mockQueue = new Mock<ICrawlQueue>();

            mockBrowserAdapter.Setup(mock => mock.GetPageAdapters())
                .Returns(new List<IPageAdapter> {mockPageAdapter.Object});
            
            mockBrowserAdapter.Setup(mock => mock.GetUserAgentAsync()).ReturnsAsync(USER_AGENT);
            mockPageAdapter.Setup(mock => mock.NavigateToAsync(It.IsAny<Uri>())).ReturnsAsync(true);
            mockPageAdapter.Setup(mock => mock.GetContentAsync()).ReturnsAsync(EXTRACTED_DATA);

            mockRobotParser.Setup(mock => mock.UriForbidden(It.IsAny<Uri>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            mockQueue
                .Setup(mock => mock.Dequeue())
                .ReturnsAsync(() => new Uri($"http://localhost/{Guid.NewGuid()}"));

            var crawlerOptions = new CrawlerOptions
            {
                RobotParser = mockRobotParser.Object
            };

            var crawler = new DistributedCrawler(mockBrowserAdapter.Object, crawlerOptions);

            var channelReader = crawler.Crawl(
                job, 
                1,
                new BloomFilter(100),
                mockQueue.Object,
                cancellationTokenSource.Token);

            var dataStore = new Dictionary<Uri, IEnumerable<string>>();

            try
            {
                while (await channelReader.WaitToReadAsync(cancellationTokenSource.Token))
                {
                    while (channelReader.TryRead(out var result))
                    {
                        if (result.Data.Count() > 0)
                        {
                            foreach (var item in result.Data)
                            {
                                dataStore.Add(item.Key, item.Value);
                            }
                        }

                        if (dataStore.Count() > 0)
                        {
                            cancellationTokenSource.Cancel();
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
            Assert.IsNotNull(dataStore.First().Key.AbsoluteUri);
            Assert.AreEqual("data", dataStore.First().Value.First());
        }
    }
}

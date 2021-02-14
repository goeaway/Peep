using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.BrowserAdapter;
using Peep.Exceptions;
using Peep.Factories;
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
    [TestCategory("Crawler - Crawler")]
    public class CrawlerTests
    {
        [TestMethod]
        public void Throws_If_CrawlerOptions_Null()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new DistributedCrawler(null));
        }

        [TestMethod]
        public void Throws_If_CrawlerOptions_BrowserAdapterFactory_Null()
        {
            var options = new CrawlerOptions
            {
                BrowserAdapterFactory = null
            };
            Assert.ThrowsException<CrawlerOptionsException>(() => new DistributedCrawler(options));
        }

        [TestMethod]
        public void Throws_If_CrawlerOptions_DataExtractor_Null()
        {
            var options = new CrawlerOptions
            {
                DataExtractor = null
            };
            Assert.ThrowsException<CrawlerOptionsException>(() => new DistributedCrawler(options));
        }

        [TestMethod]
        public void Throws_If_CrawlerOptions_RobotParser_Null()
        {
            var options = new CrawlerOptions
            {
                RobotParser = null
            };
            Assert.ThrowsException<CrawlerOptionsException>(() => new DistributedCrawler(options));
        }

        [TestMethod]
        public void Throws_If_CrawlerOptions_Filter_Null()
        {
            var options = new CrawlerOptions
            {
                Filter = null
            };
            Assert.ThrowsException<CrawlerOptionsException>(() => new DistributedCrawler(options));
        }

        [TestMethod]
        public void Throws_If_CrawlerOptions_Queue_Null()
        {
            var options = new CrawlerOptions
            {
                Filter = new BloomFilter(1000),
                Queue = null
            };
            Assert.ThrowsException<CrawlerOptionsException>(() => new DistributedCrawler(options));
        }


        [TestMethod]
        public void Crawl_Throws_If_Options_Null()
        {
            var crawler = new DistributedCrawler();
            Assert.ThrowsException<ArgumentNullException>(() => crawler.Crawl(null, default, default));
        }

        [TestMethod]
        public void Crawl_Throws_If_Seeds_Null()
        {
            var crawler = new DistributedCrawler();
            var JOB = new StoppableCrawlJob();
            Assert.ThrowsException<InvalidOperationException>(() => crawler.Crawl(JOB, default, default));
        }

        [TestMethod]
        public void Crawl_Throws_If_Seeds_Empty()
        {
            var JOB = new StoppableCrawlJob
            {
                Seeds = new List<Uri>()
            };

            var crawler = new DistributedCrawler();
            Assert.ThrowsException<InvalidOperationException>(
                () => crawler.Crawl(JOB, default, default));
        }

        [TestMethod]
        public void Stops_If_CancellationToken_Triggered()
        {
            var JOB = new StoppableCrawlJob
            {
                Seeds = new List<Uri>
                {
                    new Uri("http://localhost")
                }
            };

            var mockBrowserAdapterFactory = new Mock<IBrowserAdapterFactory>();
            var mockBrowserAdapter = new Mock<IBrowserAdapter>();
            mockBrowserAdapterFactory.Setup(mock => mock.GetBrowserAdapter())
                .ReturnsAsync(mockBrowserAdapter.Object);

            var options = new CrawlerOptions
            {
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object,
                Filter = new BloomFilter(100),
                Queue = new CrawlQueue(JOB.Seeds)
            };

            var crawler = new DistributedCrawler(options);

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var result = crawler.Crawl(JOB, default, cancellationTokenSource.Token);

            mockBrowserAdapter.Verify(
                mock => mock.NavigateToAsync(It.IsAny<Uri>()),
                Times.Never());
        }

        [TestMethod]
        public async Task Does_Not_Crawl_The_Same_Page_Twice()
        {
            var URI = new Uri("http://localhost/");
            const string EXTRACTED_DATA = "<a href='/'></a><a href='/some-other-page'></a>";
            const string USER_AGENT = "user-agent";
            var CANCELLATION_TOKEN_SOURCE = new CancellationTokenSource();
            var JOB = new StoppableCrawlJob
            {
                IgnoreRobots = false,
                Seeds = new List<Uri>
                {
                    URI
                }
            };

            var mockBrowserAdapterFactory = new Mock<IBrowserAdapterFactory>();
            var mockBrowserAdapter = new Mock<IBrowserAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();

            mockBrowserAdapterFactory.Setup(mock => mock.GetBrowserAdapter())
                .ReturnsAsync(mockBrowserAdapter.Object);

            mockBrowserAdapter.Setup(mock => mock.NavigateToAsync(It.IsAny<Uri>())).ReturnsAsync(true);
            mockBrowserAdapter.Setup(mock => mock.GetUserAgentAsync()).ReturnsAsync(USER_AGENT);
            mockBrowserAdapter.Setup(mock => mock.GetContentAsync()).ReturnsAsync(EXTRACTED_DATA);

            mockRobotParser.Setup(mock => mock.UriForbidden(It.IsAny<Uri>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            var crawlerOptions = new CrawlerOptions
            {
                RobotParser = mockRobotParser.Object,
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object,
                Filter = new BloomFilter(100),
                Queue = new CrawlQueue(JOB.Seeds)
            };

            var crawler = new DistributedCrawler(crawlerOptions);

            var result = crawler.Crawl(JOB, default, CANCELLATION_TOKEN_SOURCE.Token);

            await Task.Delay(1000);
            CANCELLATION_TOKEN_SOURCE.Cancel();

            await result.WaitToReadAsync();

            mockBrowserAdapter.Verify(
                mock => mock.NavigateToAsync(
                    It.Is<Uri>(uri => uri.AbsoluteUri == "http://localhost/some-other-page/")),
                Times.Once());
        }

        [TestMethod]
        public async Task Does_Not_Crawl_Forbidden_Page()
        {
            const string EXTRACTED_DATA = "<a href='/forbidden'></a>";
            const string USER_AGENT = "user-agent";
            var CANCELLATION_TOKEN_SOURCE = new CancellationTokenSource();
            var URI = new Uri("http://localhost");
            var JOB = new StoppableCrawlJob
            {
                Seeds = new List<Uri>
                {
                    URI
                }
            };

            var mockBrowserAdapterFactory = new Mock<IBrowserAdapterFactory>();
            var mockBrowserAdapter = new Mock<IBrowserAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();

            mockBrowserAdapterFactory.Setup(mock => mock.GetBrowserAdapter())
                .ReturnsAsync(mockBrowserAdapter.Object);

            mockBrowserAdapter.Setup(mock => mock.NavigateToAsync(URI)).ReturnsAsync(true);
            mockBrowserAdapter.Setup(mock => mock.GetUserAgentAsync()).ReturnsAsync(USER_AGENT);
            mockBrowserAdapter.Setup(mock => mock.GetContentAsync()).ReturnsAsync(EXTRACTED_DATA);

            mockRobotParser.Setup(mock => mock.UriForbidden(It.IsAny<Uri>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var crawlerOptions = new CrawlerOptions
            {
                RobotParser = mockRobotParser.Object,
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object,
                Filter = new BloomFilter(100),
                Queue = new CrawlQueue(JOB.Seeds)
            };

            var crawler = new DistributedCrawler(crawlerOptions);

            var result = crawler.Crawl(JOB, default, CANCELLATION_TOKEN_SOURCE.Token);

            await Task.Delay(1000);
            CANCELLATION_TOKEN_SOURCE.Cancel();

            await result.WaitToReadAsync();
            mockBrowserAdapter
                .Verify(
                    mock => mock.NavigateToAsync(It.Is<Uri>(uri => uri.AbsoluteUri == "http://localhost/forbidden/")),
                    Times.Never());
        }

        [TestMethod]
        public async Task Does_Crawl_Forbidden_Page_If_Ignore_Robots_Option_Is_True()
        {
            const string EXTRACTED_DATA = "<a href='/forbidden'></a>";
            const string USER_AGENT = "user-agent";
            var CANCELLATION_TOKEN_SOURCE = new CancellationTokenSource();
            var URI = new Uri("http://localhost");
            var JOB = new StoppableCrawlJob
            {
                IgnoreRobots = true,
                Seeds = new List<Uri>
                {
                    URI
                }
            };

            var mockBrowserAdapterFactory = new Mock<IBrowserAdapterFactory>();
            var mockBrowserAdapter = new Mock<IBrowserAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();

            mockBrowserAdapterFactory.Setup(mock => mock.GetBrowserAdapter())
                .ReturnsAsync(mockBrowserAdapter.Object);

            mockBrowserAdapter.Setup(mock => mock.NavigateToAsync(It.IsAny<Uri>())).ReturnsAsync(true);
            mockBrowserAdapter.Setup(mock => mock.GetUserAgentAsync()).ReturnsAsync(USER_AGENT);
            mockBrowserAdapter.Setup(mock => mock.GetContentAsync()).ReturnsAsync(EXTRACTED_DATA);

            mockRobotParser.Setup(mock => mock.UriForbidden(It.IsAny<Uri>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var crawlerOptions = new CrawlerOptions
            {
                RobotParser = mockRobotParser.Object,
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object,
                Filter = new BloomFilter(100),
                Queue = new CrawlQueue(JOB.Seeds)
            };

            var crawler = new DistributedCrawler(crawlerOptions);

            var result = crawler.Crawl(JOB, default, CANCELLATION_TOKEN_SOURCE.Token);

            await Task.Delay(1000);
            CANCELLATION_TOKEN_SOURCE.Cancel();

            await result.WaitToReadAsync();

            mockBrowserAdapter
                .Verify(
                    mock => mock.NavigateToAsync(It.Is<Uri>(uri => uri.AbsoluteUri == "http://localhost/forbidden/")),
                    Times.Once());
        }

        [TestMethod]
        public async Task Does_Not_Crawl_Page_That_Does_Not_Match_Source()
        {
            var URI = new Uri("http://localhost/");
            const string EXTRACTED_DATA = "<a href='//test.com/'></a><a href='/valid'></a>";
            const string USER_AGENT = "user-agent";
            var CANCELLATION_TOKEN_SOURCE = new CancellationTokenSource();
            var JOB = new StoppableCrawlJob
            {
                Seeds = new List<Uri>
                {
                    URI
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

            var mockBrowserAdapterFactory = new Mock<IBrowserAdapterFactory>();
            var mockBrowserAdapter = new Mock<IBrowserAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();

            mockBrowserAdapterFactory.Setup(mock => mock.GetBrowserAdapter())
                .ReturnsAsync(mockBrowserAdapter.Object);

            mockBrowserAdapter.Setup(mock => mock.NavigateToAsync(It.IsAny<Uri>())).ReturnsAsync(true);
            mockBrowserAdapter.Setup(mock => mock.GetUserAgentAsync()).ReturnsAsync(USER_AGENT);
            mockBrowserAdapter.Setup(mock => mock.GetContentAsync()).ReturnsAsync(EXTRACTED_DATA);

            mockRobotParser.Setup(mock => mock.UriForbidden(It.IsAny<Uri>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            var crawlerOptions = new CrawlerOptions
            {
                RobotParser = mockRobotParser.Object,
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object,
                Filter = new BloomFilter(100),
                Queue = new CrawlQueue(JOB.Seeds)
            };

            var crawler = new DistributedCrawler(crawlerOptions);

            var result = crawler.Crawl(JOB, default, CANCELLATION_TOKEN_SOURCE.Token);

            await Task.Delay(1000);
            CANCELLATION_TOKEN_SOURCE.Cancel();

            await result.WaitToReadAsync();
            mockBrowserAdapter
                .Verify(mock =>
                    mock.NavigateToAsync(It.Is<Uri>(uri => uri.AbsoluteUri == "https://test.com/")),
                    Times.Never());
        }

        [TestMethod]
        public async Task Does_Crawl_Page_If_URI_Matches_UriRegex_On_Same_Domain()
        {
            var URI = new Uri("http://localhost/an-area");
            const string EXTRACTED_DATA = "<a href='/different-area'></a>";
            const string USER_AGENT = "user-agent";
            var CANCELLATION_TOKEN_SOURCE = new CancellationTokenSource();
            var JOB = new StoppableCrawlJob
            {
                UriRegex = "/different-area.*?",
                Seeds = new List<Uri>
                {
                    URI
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

            var mockBrowserAdapterFactory = new Mock<IBrowserAdapterFactory>();
            var mockBrowserAdapter = new Mock<IBrowserAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();

            mockBrowserAdapterFactory.Setup(mock => mock.GetBrowserAdapter())
                .ReturnsAsync(mockBrowserAdapter.Object);

            mockBrowserAdapter.Setup(mock => mock.NavigateToAsync(It.IsAny<Uri>())).ReturnsAsync(true);
            mockBrowserAdapter.Setup(mock => mock.GetUserAgentAsync()).ReturnsAsync(USER_AGENT);
            mockBrowserAdapter.Setup(mock => mock.GetContentAsync()).ReturnsAsync(EXTRACTED_DATA);

            mockRobotParser.Setup(mock => mock.UriForbidden(It.IsAny<Uri>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            var crawlerOptions = new CrawlerOptions
            {
                RobotParser = mockRobotParser.Object,
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object,
                Filter = new BloomFilter(100),
                Queue = new CrawlQueue(JOB.Seeds)
            };

            var crawler = new DistributedCrawler(crawlerOptions);

            var result = crawler.Crawl(JOB, default, CANCELLATION_TOKEN_SOURCE.Token);

            await Task.Delay(1000);
            CANCELLATION_TOKEN_SOURCE.Cancel();

            await result.WaitToReadAsync();

            mockBrowserAdapter.Verify(
                mock => mock.NavigateToAsync(It.Is<Uri>(uri => uri.AbsoluteUri == "http://localhost/different-area/")),
                Times.Once());
        }

        [TestMethod]
        public async Task Does_Not_Crawl_Page_If_URI_Matches_UriRegex_On_Different_Domain()
        {
            var URI = new Uri("http://localhost/");
            const string EXTRACTED_DATA = "<a href='//test.com'></a>";
            const string USER_AGENT = "user-agent";
            var CANCELLATION_TOKEN_SOURCE = new CancellationTokenSource();
            var JOB = new StoppableCrawlJob
            {
                UriRegex = "//test.com*?",
                Seeds = new List<Uri>
                {
                    URI
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

            var mockBrowserAdapterFactory = new Mock<IBrowserAdapterFactory>();
            var mockBrowserAdapter = new Mock<IBrowserAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();

            mockBrowserAdapterFactory.Setup(mock => mock.GetBrowserAdapter())
                .ReturnsAsync(mockBrowserAdapter.Object);

            mockBrowserAdapter.Setup(mock => mock.NavigateToAsync(URI)).ReturnsAsync(true);
            mockBrowserAdapter.Setup(mock => mock.GetUserAgentAsync()).ReturnsAsync(USER_AGENT);
            mockBrowserAdapter.Setup(mock => mock.GetContentAsync()).ReturnsAsync(EXTRACTED_DATA);

            mockRobotParser.Setup(mock => mock.UriForbidden(It.IsAny<Uri>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            var crawlerOptions = new CrawlerOptions
            {
                RobotParser = mockRobotParser.Object,
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object,
                Filter = new BloomFilter(100),
                Queue = new CrawlQueue(JOB.Seeds)
            };

            var crawler = new DistributedCrawler(crawlerOptions);

            var result = crawler.Crawl(JOB, default, CANCELLATION_TOKEN_SOURCE.Token);

            await Task.Delay(1000);
            CANCELLATION_TOKEN_SOURCE.Cancel();

            await result.WaitToReadAsync();

            mockBrowserAdapter.Verify(
                mock => mock.NavigateToAsync(It.Is<Uri>(uri => uri.AbsoluteUri == "https://test.com/")),
                Times.Never());
        }

        [TestMethod]
        public async Task Crawl_Performs_Page_Actions_Before_Getting_Content()
        {
            var URI = new Uri("http://localhost/");
            const string USER_AGENT = "user-agent";

            var CANCELLATION_TOKEN_SOURCE = new CancellationTokenSource();

            var mockPageAction = new Mock<IPageAction>();

            var JOB = new StoppableCrawlJob
            {
                Seeds = new List<Uri>
                {
                    URI
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

            var mockBrowserAdapterFactory = new Mock<IBrowserAdapterFactory>();
            var mockBrowserAdapter = new Mock<IBrowserAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();

            mockBrowserAdapterFactory.Setup(mock => mock.GetBrowserAdapter())
                .ReturnsAsync(mockBrowserAdapter.Object);

            mockBrowserAdapter.Setup(mock => mock.NavigateToAsync(URI)).ReturnsAsync(true);
            mockBrowserAdapter.Setup(mock => mock.GetUserAgentAsync()).ReturnsAsync(USER_AGENT);

            var crawlerOptions = new CrawlerOptions
            {
                RobotParser = mockRobotParser.Object,
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object,
                Filter = new BloomFilter(100),
                Queue = new CrawlQueue(JOB.Seeds)
            };

            var crawler = new DistributedCrawler(crawlerOptions);

            var result = crawler.Crawl(JOB, default, CANCELLATION_TOKEN_SOURCE.Token);

            await Task.Delay(1000);
            CANCELLATION_TOKEN_SOURCE.Cancel();

            await result.WaitToReadAsync();
            mockPageAction.Verify(mock => mock.Perform(mockBrowserAdapter.Object), Times.Once());
        }

        [TestMethod]
        public async Task Crawl_Only_Performs_Page_Action_If_Page_URL_Matches_Uri_Regex()
        {
            var URI = new Uri("http://localhost/");
            const string USER_AGENT = "user-agent";
            var CANCELLATION_TOKEN_SOURCE = new CancellationTokenSource();

            var mockPageAction = new Mock<IPageAction>();
            mockPageAction.Setup(mock => mock.UriRegex).Returns("anything");

            var JOB = new StoppableCrawlJob
            {
                Seeds = new List<Uri>
                {
                    URI
                },
                PageActions = new List<IPageAction>
                {
                    mockPageAction.Object
                }
            };

            var mockBrowserAdapterFactory = new Mock<IBrowserAdapterFactory>();
            var mockBrowserAdapter = new Mock<IBrowserAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();

            mockBrowserAdapterFactory.Setup(mock => mock.GetBrowserAdapter())
                .ReturnsAsync(mockBrowserAdapter.Object);

            mockBrowserAdapter.Setup(mock => mock.NavigateToAsync(URI)).ReturnsAsync(true);
            mockBrowserAdapter.Setup(mock => mock.GetUserAgentAsync()).ReturnsAsync(USER_AGENT);

            var crawlerOptions = new CrawlerOptions
            {
                RobotParser = mockRobotParser.Object,
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object,
                Filter = new BloomFilter(100),
                Queue = new CrawlQueue(JOB.Seeds)
            };

            var crawler = new DistributedCrawler(crawlerOptions);

            var result = crawler.Crawl(JOB, default, CANCELLATION_TOKEN_SOURCE.Token);

            await Task.Delay(1000);
            CANCELLATION_TOKEN_SOURCE.Cancel();

            await result.WaitToReadAsync();
            mockPageAction.Verify(mock => mock.Perform(mockBrowserAdapter.Object), Times.Never());
        }

        [TestMethod]
        public async Task Crawl_Should_Periodically_Write_Data_To_Channel()
        {
            var URI = new Uri("http://localhost/");
            const string EXTRACTED_DATA = "<a href='//test.com/'></a>data";
            const string USER_AGENT = "user-agent";
            var CANCELLATION_TOKEN_SOURCE = new CancellationTokenSource();
            var JOB = new StoppableCrawlJob
            {
                Seeds = new List<Uri>
                {
                    URI
                },
                DataRegex = "(?<data>data)"
            };

            var mockBrowserAdapterFactory = new Mock<IBrowserAdapterFactory>();
            var mockBrowserAdapter = new Mock<IBrowserAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();
            var mockQueue = new Mock<ICrawlQueue>();

            mockBrowserAdapterFactory.Setup(mock => mock.GetBrowserAdapter())
                .ReturnsAsync(mockBrowserAdapter.Object);

            mockBrowserAdapter.Setup(mock => mock.NavigateToAsync(It.IsAny<Uri>())).ReturnsAsync(true);
            mockBrowserAdapter.Setup(mock => mock.GetUserAgentAsync()).ReturnsAsync(USER_AGENT);
            mockBrowserAdapter.Setup(mock => mock.GetContentAsync()).ReturnsAsync(EXTRACTED_DATA);

            mockRobotParser.Setup(mock => mock.UriForbidden(It.IsAny<Uri>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            mockQueue
                .Setup(mock => mock.Dequeue())
                .ReturnsAsync(() => new Uri($"http://localhost/{Guid.NewGuid()}"));

            var crawlerOptions = new CrawlerOptions
            {
                RobotParser = mockRobotParser.Object,
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object,
                Filter = new BloomFilter(100),
                Queue = mockQueue.Object
            };

            var crawler = new DistributedCrawler(crawlerOptions);

            var channelReader = crawler.Crawl(JOB, TimeSpan.FromMilliseconds(10), CANCELLATION_TOKEN_SOURCE.Token);

            var dataStore = new Dictionary<Uri, IEnumerable<string>>();

            try
            {
                await foreach (var result in channelReader.ReadAllAsync(CANCELLATION_TOKEN_SOURCE.Token))
                {
                    if (result.Data.Count > 0)
                    {
                        foreach(var item in result.Data)
                        {
                            dataStore.Add(item.Key, item.Value);
                        }
                    }

                    if (dataStore.Count > 0)
                    {
                        CANCELLATION_TOKEN_SOURCE.Cancel();
                    }
                }
            }
            catch (Exception)
            {

            }

            Assert.IsNotNull(dataStore.First().Key.AbsoluteUri);
            Assert.AreEqual("data", dataStore.First().Value.First());
        }
    }
}

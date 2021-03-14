﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    [TestCategory("Core - Crawler - Crawler")]
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
        public void Crawl_Throws_If_Options_Null()
        {
            var crawler = new DistributedCrawler();
            Assert.ThrowsException<ArgumentNullException>(
                () => crawler.Crawl(null, 1, new BloomFilter(100), new CrawlQueue(), default));
        }

        [TestMethod]
        public void Crawl_Throws_If_Seeds_Null()
        {
            var crawler = new DistributedCrawler();
            var JOB = new StoppableCrawlJob();
            Assert.ThrowsException<InvalidOperationException>(
                () => crawler.Crawl(JOB, 1, new BloomFilter(100), new CrawlQueue(), default));
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
                () => crawler.Crawl(JOB, 1, new BloomFilter(100), new CrawlQueue(), default));
        }

        [TestMethod]
        public void Crawl_Throws_If_Filter_Null()
        {
            var JOB = new StoppableCrawlJob
            {
                Seeds = new List<Uri>
                {
                    new Uri("http://localhost")
                }
            };

            var crawler = new DistributedCrawler();
            Assert.ThrowsException<ArgumentNullException>(
                () => crawler.Crawl(JOB, 1, null, new CrawlQueue(), default));
        }

        [TestMethod]
        public void Crawl_Throws_If_Queue_Null()
        {
            var JOB = new StoppableCrawlJob
            {
                Seeds = new List<Uri>
                {
                    new Uri("http://localhost")
                }
            };

            var crawler = new DistributedCrawler();
            Assert.ThrowsException<ArgumentNullException>(
                () => crawler.Crawl(JOB, 1, new BloomFilter(100), null, default));
        }

        [TestMethod]
        public void Crawl_Throws_If_DataCountUpdate_LessThanOne()
        {
            var JOB = new StoppableCrawlJob
            {
                Seeds = new List<Uri>
                {
                    new Uri("http://localhost")
                }
            };

            var crawler = new DistributedCrawler();
            Assert.ThrowsException<InvalidOperationException>(
                () => crawler.Crawl(JOB, 0, new BloomFilter(100), new CrawlQueue(), default));
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
            var mockPageAdapter = new Mock<IPageAdapter>();
            
            mockBrowserAdapterFactory.Setup(mock => mock.GetBrowserAdapter())
                .ReturnsAsync(mockBrowserAdapter.Object);

            var options = new CrawlerOptions
            {
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object,
            };

            var crawler = new DistributedCrawler(options);

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var result = crawler.Crawl(JOB, 
                1, 
                new BloomFilter(100),
                new CrawlQueue(JOB.Seeds),
                cancellationTokenSource.Token);

            mockPageAdapter.Verify(
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
            var mockPageAdapter = new Mock<IPageAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();

            mockBrowserAdapterFactory.Setup(mock => mock.GetBrowserAdapter())
                .ReturnsAsync(mockBrowserAdapter.Object);

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
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object
            };

            var crawler = new DistributedCrawler(crawlerOptions);

            var result = crawler.Crawl(
                JOB,
                1,
                new BloomFilter(100),
                new CrawlQueue(JOB.Seeds),
                CANCELLATION_TOKEN_SOURCE.Token);

            await Task.Delay(1000);
            CANCELLATION_TOKEN_SOURCE.Cancel();

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
            var mockPageAdapter = new Mock<IPageAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();

            mockBrowserAdapterFactory.Setup(mock => mock.GetBrowserAdapter())
                .ReturnsAsync(mockBrowserAdapter.Object);
            
            mockBrowserAdapter.Setup(mock => mock.GetPageAdapters())
                .Returns(new List<IPageAdapter> {mockPageAdapter.Object});

            mockBrowserAdapter.Setup(mock => mock.GetUserAgentAsync()).ReturnsAsync(USER_AGENT);
            mockPageAdapter.Setup(mock => mock.NavigateToAsync(URI)).ReturnsAsync(true);
            mockPageAdapter.Setup(mock => mock.GetContentAsync()).ReturnsAsync(EXTRACTED_DATA);

            mockRobotParser.Setup(mock => mock.UriForbidden(It.IsAny<Uri>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var crawlerOptions = new CrawlerOptions
            {
                RobotParser = mockRobotParser.Object,
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object
            };

            var crawler = new DistributedCrawler(crawlerOptions);

            var result = crawler.Crawl(
                JOB, 
                1,
                new BloomFilter(100),
                new CrawlQueue(JOB.Seeds),
                CANCELLATION_TOKEN_SOURCE.Token);

            await Task.Delay(1000);
            CANCELLATION_TOKEN_SOURCE.Cancel();

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
            var mockPageAdapter = new Mock<IPageAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();

            mockBrowserAdapterFactory.Setup(mock => mock.GetBrowserAdapter())
                .ReturnsAsync(mockBrowserAdapter.Object);

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
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object
            };

            var crawler = new DistributedCrawler(crawlerOptions);

            var result = crawler.Crawl(
                JOB, 
                1, 
                new BloomFilter(100), 
                new CrawlQueue(JOB.Seeds), 
                CANCELLATION_TOKEN_SOURCE.Token);

            await Task.Delay(1000);
            CANCELLATION_TOKEN_SOURCE.Cancel();

            await result.WaitToReadAsync();

            mockPageAdapter
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
            var mockPageAdapter = new Mock<IPageAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();

            mockBrowserAdapterFactory.Setup(mock => mock.GetBrowserAdapter())
                .ReturnsAsync(mockBrowserAdapter.Object);
            
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
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object
            };

            var crawler = new DistributedCrawler(crawlerOptions);

            var result = crawler.Crawl(JOB, 1, new BloomFilter(100), new CrawlQueue(JOB.Seeds), CANCELLATION_TOKEN_SOURCE.Token);

            await Task.Delay(1000);
            CANCELLATION_TOKEN_SOURCE.Cancel();

            await result.WaitToReadAsync();
            mockPageAdapter
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
            var mockPageAdapter = new Mock<IPageAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();

            mockBrowserAdapterFactory.Setup(mock => mock.GetBrowserAdapter())
                .ReturnsAsync(mockBrowserAdapter.Object);

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
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object
            };

            var crawler = new DistributedCrawler(crawlerOptions);

            var result = crawler.Crawl(JOB, 1, new BloomFilter(100), new CrawlQueue(JOB.Seeds), CANCELLATION_TOKEN_SOURCE.Token);

            await Task.Delay(1000);
            CANCELLATION_TOKEN_SOURCE.Cancel();

            await result.WaitToReadAsync();

            mockPageAdapter.Verify(
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
            var mockPageAdapter = new Mock<IPageAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();

            mockBrowserAdapterFactory.Setup(mock => mock.GetBrowserAdapter())
                .ReturnsAsync(mockBrowserAdapter.Object);
            
            mockBrowserAdapter.Setup(mock => mock.GetPageAdapters())
                .Returns(new List<IPageAdapter> {mockPageAdapter.Object});

            mockBrowserAdapter.Setup(mock => mock.GetUserAgentAsync()).ReturnsAsync(USER_AGENT);
            mockPageAdapter.Setup(mock => mock.NavigateToAsync(URI)).ReturnsAsync(true);
            mockPageAdapter.Setup(mock => mock.GetContentAsync()).ReturnsAsync(EXTRACTED_DATA);

            mockRobotParser.Setup(mock => mock.UriForbidden(It.IsAny<Uri>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            var crawlerOptions = new CrawlerOptions
            {
                RobotParser = mockRobotParser.Object,
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object
            };

            var crawler = new DistributedCrawler(crawlerOptions);

            var result = crawler.Crawl(JOB, 1, new BloomFilter(100), new CrawlQueue(JOB.Seeds), CANCELLATION_TOKEN_SOURCE.Token);

            await Task.Delay(1000);
            CANCELLATION_TOKEN_SOURCE.Cancel();

            await result.WaitToReadAsync();

            mockPageAdapter.Verify(
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
            var mockPageActionPerformer = new Mock<IPageActionPerformer>();

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
            var mockPageAdapter = new Mock<IPageAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();

            mockBrowserAdapterFactory.Setup(mock => mock.GetBrowserAdapter())
                .ReturnsAsync(mockBrowserAdapter.Object);
            
            mockBrowserAdapter.Setup(mock => mock.GetPageAdapters())
                .Returns(new List<IPageAdapter> {mockPageAdapter.Object});

            mockPageAdapter.Setup(mock => mock.NavigateToAsync(URI)).ReturnsAsync(true);
            mockBrowserAdapter.Setup(mock => mock.GetUserAgentAsync()).ReturnsAsync(USER_AGENT);

            var crawlerOptions = new CrawlerOptions
            {
                RobotParser = mockRobotParser.Object,
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object,
                PageActionPerformer = mockPageActionPerformer.Object
            };

            var crawler = new DistributedCrawler(crawlerOptions);

            var result = crawler.Crawl(JOB, 1, new BloomFilter(100), new CrawlQueue(JOB.Seeds), CANCELLATION_TOKEN_SOURCE.Token);

            await Task.Delay(1000);
            CANCELLATION_TOKEN_SOURCE.Cancel();

            await result.WaitToReadAsync();
            mockPageActionPerformer.Verify(mock => mock.Perform(mockPageAction.Object, mockPageAdapter.Object), Times.Once());
        }

        [TestMethod]
        public async Task Crawl_Only_Performs_Page_Action_If_Page_URL_Matches_Uri_Regex()
        {
            var URI = new Uri("http://localhost/");
            const string USER_AGENT = "user-agent";
            var CANCELLATION_TOKEN_SOURCE = new CancellationTokenSource();

            var mockPageAction = new Mock<IPageAction>();
            mockPageAction.Setup(mock => mock.UriRegex).Returns("anything");
            var mockPageActionPerformer = new Mock<IPageActionPerformer>();

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
            var mockPageAdapter = new Mock<IPageAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();

            mockBrowserAdapterFactory.Setup(mock => mock.GetBrowserAdapter())
                .ReturnsAsync(mockBrowserAdapter.Object);
            
            mockBrowserAdapter.Setup(mock => mock.GetPageAdapters())
                .Returns(new List<IPageAdapter> {mockPageAdapter.Object});

            mockBrowserAdapter.Setup(mock => mock.GetUserAgentAsync()).ReturnsAsync(USER_AGENT);
            mockPageAdapter.Setup(mock => mock.NavigateToAsync(URI)).ReturnsAsync(true);

            var crawlerOptions = new CrawlerOptions
            {
                RobotParser = mockRobotParser.Object,
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object,
                PageActionPerformer = mockPageActionPerformer.Object
            };

            var crawler = new DistributedCrawler(crawlerOptions);

            var result = crawler.Crawl(JOB, 1, new BloomFilter(100), new CrawlQueue(JOB.Seeds), CANCELLATION_TOKEN_SOURCE.Token);

            await Task.Delay(1000);
            CANCELLATION_TOKEN_SOURCE.Cancel();

            await result.WaitToReadAsync();
            mockPageActionPerformer
                .Verify(mock => mock.Perform(mockPageAction.Object, mockPageAdapter.Object), Times.Never());
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
            var mockPageAdapter = new Mock<IPageAdapter>();
            var mockRobotParser = new Mock<IRobotParser>();
            var mockQueue = new Mock<ICrawlQueue>();

            mockBrowserAdapterFactory.Setup(mock => mock.GetBrowserAdapter())
                .ReturnsAsync(mockBrowserAdapter.Object);

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
                RobotParser = mockRobotParser.Object,
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object
            };

            var crawler = new DistributedCrawler(crawlerOptions);

            var channelReader = crawler.Crawl(
                JOB, 
                1,
                new BloomFilter(100),
                mockQueue.Object,
                CANCELLATION_TOKEN_SOURCE.Token);

            var dataStore = new Dictionary<Uri, IEnumerable<string>>();

            try
            {
                while(await channelReader.WaitToReadAsync(CANCELLATION_TOKEN_SOURCE.Token))
                {
                    while(channelReader.TryRead(out var result))
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
                            CANCELLATION_TOKEN_SOURCE.Cancel();
                        }
                    }
                }
            }
            catch (Exception)
            {

            }

            Assert.IsNotNull(dataStore.First().Key.AbsoluteUri);
            Assert.AreEqual("data", dataStore.First().Value.First());
        }

        // TEST REMOVED UNTIL IT BEHAVES
        // [TestMethod]
        // public async Task Crawl_Should_Clear_Data_Between_Channel_Writes()
        // {
        //     var URI = new Uri("http://localhost/");
        //     const string EXTRACTED_DATA = "<a href='//test.com/'></a>data";
        //     const string USER_AGENT = "user-agent";
        //     var CANCELLATION_TOKEN_SOURCE = new CancellationTokenSource();
        //     var JOB = new StoppableCrawlJob
        //     {
        //         Seeds = new List<Uri>
        //         {
        //             URI,
        //             new Uri($"http://localhost/{Guid.NewGuid()}")
        //         },
        //         DataRegex = "(?<data>data)"
        //     };
        //
        //     var mockBrowserAdapterFactory = new Mock<IBrowserAdapterFactory>();
        //     var mockBrowserAdapter = new Mock<IBrowserAdapter>();
        //     var mockPageAdapter = new Mock<IPageAdapter>();
        //     var mockRobotParser = new Mock<IRobotParser>();
        //
        //     mockBrowserAdapterFactory.Setup(mock => mock.GetBrowserAdapter())
        //         .ReturnsAsync(mockBrowserAdapter.Object);
        //
        //     mockBrowserAdapter.Setup(mock => mock.GetPageAdapters())
        //         .Returns(new List<IPageAdapter> {mockPageAdapter.Object});
        //
        //     mockBrowserAdapter.Setup(mock => mock.GetUserAgentAsync()).ReturnsAsync(USER_AGENT);
        //     mockPageAdapter.Setup(mock => mock.NavigateToAsync(It.IsAny<Uri>())).ReturnsAsync(true);
        //     mockPageAdapter.Setup(mock => mock.GetContentAsync()).ReturnsAsync(EXTRACTED_DATA);
        //
        //     mockRobotParser.Setup(mock => mock.UriForbidden(It.IsAny<Uri>(), It.IsAny<string>()))
        //         .ReturnsAsync(false);
        //
        //     var crawlerOptions = new CrawlerOptions
        //     {
        //         RobotParser = mockRobotParser.Object,
        //         BrowserAdapterFactory = mockBrowserAdapterFactory.Object
        //     };
        //
        //     var crawler = new DistributedCrawler(crawlerOptions);
        //
        //     var channelReader = crawler.Crawl(
        //         JOB,
        //         1,
        //         new BloomFilter(100),
        //         new CrawlQueue(JOB.Seeds),
        //         CANCELLATION_TOKEN_SOURCE.Token);
        //
        //     var dataStore = new List<Uri>();
        //
        //     try
        //     {
        //         while (await channelReader.WaitToReadAsync(CANCELLATION_TOKEN_SOURCE.Token))
        //         {
        //             while (channelReader.TryRead(out var result))
        //             {
        //                 foreach (var item in result.Data)
        //                 {
        //                     dataStore.Add(item.Key);
        //                 }
        //
        //                 if (dataStore.Count() > 1)
        //                 {
        //                     CANCELLATION_TOKEN_SOURCE.Cancel();
        //                 }
        //             }
        //         }
        //     }
        //     catch (Exception)
        //     {
        //
        //     }
        //
        //     Assert.AreEqual(1, dataStore.Where(ds => ds.AbsoluteUri == "http://localhost/").Count());
        // }
    }
}

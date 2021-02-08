using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.BrowserAdapter;
using Peep.Exceptions;
using Peep.Factories;
using Peep.Core.PageActions;
using Peep.Core.StopConditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Peep.Core.BrowserAdapter;
using Peep.Core;

namespace Peep.Tests
{
    [TestClass]
    [TestCategory("Crawler - Crawler")]
    public class CrawlerTests
    {
        [TestMethod]
        public void Throws_If_CrawlerOptions_Null()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new Crawler(null));
        }

        [TestMethod]
        public void Throws_If_CrawlerOptions_BrowserAdapterFactory_Null()
        {
            var options = new CrawlerOptions
            {
                BrowserAdapterFactory = null
            };
            Assert.ThrowsException<CrawlerOptionsException>(() => new Crawler(options));
        }

        [TestMethod]
        public void Throws_If_CrawlerOptions_DataExtractor_Null()
        {
            var options = new CrawlerOptions
            {
                DataExtractor = null
            };
            Assert.ThrowsException<CrawlerOptionsException>(() => new Crawler(options));
        }

        [TestMethod]
        public void Throws_If_CrawlerOptions_RobotParser_Null()
        {
            var options = new CrawlerOptions
            {
                RobotParser = null
            };
            Assert.ThrowsException<CrawlerOptionsException>(() => new Crawler(options));
        }

        [TestMethod]
        public void Throws_If_CrawlerOptions_Filter_Null()
        {
            var options = new CrawlerOptions
            {
                Filter = null
            };
            Assert.ThrowsException<CrawlerOptionsException>(() => new Crawler(options));
        }

        [TestMethod]
        public async Task Crawl_Throws_If_Options_Null()
        {
            var crawler = new Crawler();
            var CANCELLATION_TOKEN = new CancellationTokenSource().Token;
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => crawler.Crawl(null, CANCELLATION_TOKEN));
        }

        [TestMethod]
        public async Task Crawl_Throws_If_Seeds_Null()
        {
            var crawler = new Crawler();
            var JOB = new CrawlJob();
            var CANCELLATION_TOKEN = new CancellationTokenSource().Token;
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => crawler.Crawl(JOB, CANCELLATION_TOKEN));
        }

        [TestMethod]
        public async Task Crawl_Throws_If_Seeds_Empty()
        {
            var JOB = new CrawlJob
            {
                Seeds = new List<Uri>()
            };

            var CANCELLATION_TOKEN = new CancellationTokenSource().Token;

            var crawler = new Crawler();
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => crawler.Crawl(
                    JOB,
                    CANCELLATION_TOKEN));
        }

        [TestMethod]
        public async Task Stops_If_CancellationToken_Triggered()
        {
            var JOB = new CrawlJob
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
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object
            };

            var crawler = new Crawler(options);

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var result = await crawler.Crawl(JOB, cancellationTokenSource.Token);

            mockBrowserAdapter.Verify(
                mock => mock.NavigateToAsync(It.IsAny<Uri>()), 
                Times.Never());
        } 

        [TestMethod]
        public async Task Stops_If_One_StopCondition_For_Crawl_Triggered()
        {
            var JOB = new CrawlJob
            {
                Seeds = new List<Uri>
                {
                    new Uri("http://localhost")
                },
                StopConditions = new List<SerialisableStopCondition>
                {
                    new SerialisableStopCondition 
                    { 
                        Value = 0, 
                        Type = SerialisableStopConditionType.MaxDurationSeconds
                    },
                    new SerialisableStopCondition
                    {
                        Value = 1000,
                        Type = SerialisableStopConditionType.MaxCrawlCount
                    }
                }
            };

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

            var result = await crawler.Crawl(JOB, cancellationTokenSource.Token);

            mockBrowserAdapter.Verify(
                mock => mock.NavigateToAsync(It.IsAny<Uri>()),
                Times.Never());
        }

        [TestMethod]
        public async Task Returns_Crawl_Result_With_Data_CrawlCount_Duration()
        {
            var EXTRACTED_DATA = "extracted data";
            var URI = new Uri("http://localhost");
            var JOB = new CrawlJob
            {
                DataRegex = $"(?<data>{EXTRACTED_DATA})",
                Seeds = new List<Uri>
                {
                    URI
                },
                StopConditions = new List<SerialisableStopCondition>
                {
                    new SerialisableStopCondition
                    {
                        Value = 1,
                        Type = SerialisableStopConditionType.MaxDataCount
                    }
                }
            };

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

            var result = await crawler.Crawl(JOB, cancellationTokenSource.Token);

            Assert.AreEqual(1, result.CrawlCount);
            Assert.AreEqual(1, result.Data.Count);
            Assert.AreNotEqual(TimeSpan.MinValue, result.Duration);

            Assert.AreEqual(new Uri("http://localhost/"), result.Data.First().Key);
            Assert.AreEqual(EXTRACTED_DATA, result.Data.First().Value.First());
        }

        [TestMethod]
        public async Task Does_Not_Crawl_The_Same_Page_Twice()
        {
            var URI = new Uri("http://localhost/");
            const string EXTRACTED_DATA = "<a href='/'></a>";
            const string USER_AGENT = "user-agent";
            var CANCELLATION_TOKEN = new CancellationTokenSource().Token;
            var JOB = new CrawlJob
            {
                Seeds = new List<Uri>
                {
                    URI
                },
                StopConditions = new List<SerialisableStopCondition>
                {
                    new SerialisableStopCondition
                    {
                        Value = 1,
                        Type = SerialisableStopConditionType.MaxDurationSeconds
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
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object
            };

            var crawler = new Crawler(crawlerOptions);

            var result = await crawler.Crawl(JOB, CANCELLATION_TOKEN);

            mockBrowserAdapter.Verify(mock => mock.NavigateToAsync(It.IsAny<Uri>()), Times.Once());
        }

        [TestMethod]
        public async Task Does_Not_Crawl_Forbidden_Page()
        {
            const string EXTRACTED_DATA = "<a href='/forbidden'></a>";
            const string USER_AGENT = "user-agent";
            var CANCELLATION_TOKEN = new CancellationTokenSource().Token;
            var URI = new Uri("http://localhost");
            var JOB = new CrawlJob
            {
                Seeds = new List<Uri>
                {
                    URI
                },
                StopConditions = new List<SerialisableStopCondition>
                {
                    new SerialisableStopCondition
                    {
                        Value = 1,
                        Type = SerialisableStopConditionType.MaxDurationSeconds
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
                .ReturnsAsync(true);

            var crawlerOptions = new CrawlerOptions
            {
                RobotParser = mockRobotParser.Object,
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object
            };

            var crawler = new Crawler(crawlerOptions);

            var result = await crawler.Crawl(JOB, CANCELLATION_TOKEN);

            mockBrowserAdapter.Verify(mock => mock.NavigateToAsync(It.IsAny<Uri>()), Times.Once());
            mockRobotParser.Verify(mock => mock.UriForbidden(It.IsAny<Uri>(), It.IsAny<string>()), Times.Once());
        }

        [TestMethod]
        public async Task Does_Crawl_Forbidden_Page_If_Ignore_Robots_Option_Is_True()
        {
            const string EXTRACTED_DATA = "<a href='/forbidden'></a>";
            const string USER_AGENT = "user-agent";
            var CANCELLATION_TOKEN = new CancellationTokenSource().Token;
            var URI = new Uri("http://localhost");
            var JOB = new CrawlJob
            {
                IgnoreRobots = true,
                Seeds = new List<Uri>
                {
                    URI
                },
                StopConditions = new List<SerialisableStopCondition>
                {
                    new SerialisableStopCondition
                    {
                        Value = 1,
                        Type = SerialisableStopConditionType.MaxDurationSeconds
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
                .ReturnsAsync(true);

            var crawlerOptions = new CrawlerOptions
            {
                RobotParser = mockRobotParser.Object,
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object
            };

            var crawler = new Crawler(crawlerOptions);

            var result = await crawler.Crawl(JOB, CANCELLATION_TOKEN);

            mockBrowserAdapter.Verify(mock => mock.NavigateToAsync(It.IsAny<Uri>()), Times.Exactly(2));
            mockRobotParser.Verify(mock => mock.UriForbidden(It.IsAny<Uri>(), It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        public async Task Does_Not_Crawl_Page_That_Does_Not_Match_Source()
        {
            var URI = new Uri("http://localhost/");
            const string EXTRACTED_DATA = "<a href='//test.com/'></a>";
            const string USER_AGENT = "user-agent";
            var CANCELLATION_TOKEN = new CancellationTokenSource().Token;
            var JOB = new CrawlJob
            {
                Seeds = new List<Uri>
                {
                    URI
                },
                StopConditions = new List<SerialisableStopCondition>
                {
                    new SerialisableStopCondition
                    {
                        Value = 1,
                        Type = SerialisableStopConditionType.MaxDurationSeconds
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
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object
            };

            var crawler = new Crawler(crawlerOptions);

            var result = await crawler.Crawl(JOB, CANCELLATION_TOKEN);

            mockBrowserAdapter.Verify(mock => mock.NavigateToAsync(It.IsAny<Uri>()), Times.Once());
        }

        [TestMethod]
        public async Task Does_Crawl_Page_If_URI_Matches_UriRegex_On_Same_Domain()
        {
            var URI = new Uri("http://localhost/an-area");
            const string EXTRACTED_DATA = "<a href='/different-area'></a>";
            const string USER_AGENT = "user-agent";
            var CANCELLATION_TOKEN = new CancellationTokenSource().Token;
            var JOB = new CrawlJob
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
                        Value = 1,
                        Type = SerialisableStopConditionType.MaxDurationSeconds
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
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object
            };

            var crawler = new Crawler(crawlerOptions);

            var result = await crawler.Crawl(JOB, CANCELLATION_TOKEN);

            mockBrowserAdapter.Verify(mock => mock.NavigateToAsync(It.IsAny<Uri>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task Does_Not_Crawl_Page_If_URI_Matches_UriRegex_On_Different_Domain()
        {
            var URI = new Uri("http://localhost/");
            const string EXTRACTED_DATA = "<a href='//test.com'></a>";
            const string USER_AGENT = "user-agent";
            var CANCELLATION_TOKEN = new CancellationTokenSource().Token;
            var JOB = new CrawlJob
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
                        Value = 1,
                        Type = SerialisableStopConditionType.MaxDurationSeconds
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
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object
            };

            var crawler = new Crawler(crawlerOptions);

            var result = await crawler.Crawl(JOB, CANCELLATION_TOKEN);

            mockBrowserAdapter.Verify(mock => mock.NavigateToAsync(It.IsAny<Uri>()), Times.Once());
        }

        [TestMethod]
        public async Task Crawl_Performs_Page_Actions_Before_Getting_Content()
        {
            var URI = new Uri("http://localhost/");
            const string USER_AGENT = "user-agent";

            var CANCELLATION_TOKEN = new CancellationTokenSource().Token;

            var mockPageAction = new Mock<IPageAction>();

            var JOB = new CrawlJob
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
                        Type = SerialisableStopConditionType.MaxDurationSeconds
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
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object
            };

            var crawler = new Crawler(crawlerOptions);

            var result = await crawler.Crawl(JOB, CANCELLATION_TOKEN);

            mockPageAction.Verify(mock => mock.Perform(mockBrowserAdapter.Object), Times.Once());
        }

        [TestMethod]
        public async Task Crawl_Only_Performs_Page_Action_If_Page_URL_Matches_Uri_Regex()
        {
            var URI = new Uri("http://localhost/");
            const string USER_AGENT = "user-agent";
            var CANCELLATION_TOKEN = new CancellationTokenSource().Token;

            var mockPageAction = new Mock<IPageAction>();
            mockPageAction.Setup(mock => mock.UriRegex).Returns("anything");

            var JOB = new CrawlJob
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
                        Type = SerialisableStopConditionType.MaxDurationSeconds
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
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object
            };

            var crawler = new Crawler(crawlerOptions);

            var result = await crawler.Crawl(JOB, CANCELLATION_TOKEN);

            mockPageAction.Verify(mock => mock.Perform(mockBrowserAdapter.Object), Times.Never());
        }

        [TestMethod]
        public async Task Crawl_Calls_ProgressUpdate_Periodically_From_TimeSpan()
        {
            var URI = new Uri("http://localhost/");
            const string EXTRACTED_DATA = "<a href='//test.com/'></a>";
            const string USER_AGENT = "user-agent";
            var CANCELLATION_TOKEN = new CancellationTokenSource().Token;
            var JOB = new CrawlJob
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
                        Type = SerialisableStopConditionType.MaxDurationSeconds
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
                BrowserAdapterFactory = mockBrowserAdapterFactory.Object
            };

            var crawler = new Crawler(crawlerOptions);
            var shouldFail = true;

            // not the best way to assert but whatever
            var result = await crawler.Crawl(
                JOB, 
                TimeSpan.FromSeconds(1),
                progress => {
                    Assert.IsNotNull(progress);
                    Assert.AreEqual(1, progress.CrawlCount);
                    Assert.AreEqual(0, progress.DataCount);
                    shouldFail = false;
                },
                CANCELLATION_TOKEN);

            if(shouldFail)
            {
                Assert.Fail("progress updater was not called");
            }
        }

    }
}

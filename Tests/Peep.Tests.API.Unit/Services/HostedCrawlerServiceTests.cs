using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Peep.API.Application;
using Peep.API.Application.Options;
using Peep.API.Application.Providers;
using Peep.API.Models.Entities;
using Peep.Core;
using Peep.Exceptions;
using Peep.Tests.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Peep.Tests.API.Unit.Services
{
    [TestClass]
    [TestCategory("API - Unit - Hosted Crawler Service")]
    public class HostedCrawlerServiceTests
    {
        private readonly CrawlConfigOptions _options = new CrawlConfigOptions 
        { 
            ProgressUpdateMilliseconds = 100 
        };


        [TestMethod]
        public async Task Execute_Completes_When_CancellationToken_Cancelled()
        {
            using var context = Setup.CreateContext();
            var logger = new LoggerConfiguration().CreateLogger();

            var mockCrawler = new Mock<ICrawler>();
            var nowProvider = new NowProvider();

            var service = new HostedCrawlerService(context, logger, mockCrawler.Object, nowProvider, _options);

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await service.StopAsync(cancellationTokenSource.Token);
            // if the test just goes on forever we consider it failed...
        }

        [TestMethod]
        public async Task Uses_Crawler_When_Job_Found()
        {
            const string JOB_ID = "job id";
            var DATE_QUEUED = new DateTime(2021, 01, 01);
            var JOB = new CrawlJob
            {
                Seeds = new List<Uri>
                {
                    new Uri("http://localhost")
                }
            };

            using var context = Setup.CreateContext();
            var logger = new LoggerConfiguration().CreateLogger();

            var mockCrawler = new Mock<ICrawler>();
            mockCrawler.Setup(mock => mock.Crawl(
                    It.IsAny<CrawlJob>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<Action<CrawlResult>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CrawlResult());

            var nowProvider = new NowProvider();

            var service = new HostedCrawlerService(context, logger, mockCrawler.Object, nowProvider, _options);

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            context.QueuedJobs.Add(new QueuedJob
            {
                JobJson = JsonConvert.SerializeObject(JOB),
                Id = JOB_ID,
                DateQueued = DATE_QUEUED
            });

            context.SaveChanges();

            await Task.Delay(1000);

            await service.StopAsync(cancellationTokenSource.Token);
            mockCrawler.Verify(mock => mock.Crawl(
                It.IsAny<CrawlJob>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<Action<CrawlResult>>(),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [TestMethod]
        public async Task Removes_Queued_Job_From_Db_Upon_Pickup()
        {
            const string JOB_ID = "job id";
            var DATE_QUEUED = new DateTime(2021, 01, 01);
            var JOB = new CrawlJob
            {
                Seeds = new List<Uri>
                {
                    new Uri("http://localhost")
                }
            };

            using var context = Setup.CreateContext();
            var logger = new LoggerConfiguration().CreateLogger();

            var mockCrawler = new Mock<ICrawler>();
            mockCrawler.Setup(mock => mock.Crawl(
                    It.IsAny<CrawlJob>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<Action<CrawlResult>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CrawlResult());

            var nowProvider = new NowProvider();

            var service = new HostedCrawlerService(context, logger, mockCrawler.Object, nowProvider, _options);

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            context.QueuedJobs.Add(new QueuedJob
            {
                JobJson = JsonConvert.SerializeObject(JOB),
                Id = JOB_ID,
                DateQueued = DATE_QUEUED
            });

            context.SaveChanges();

            await Task.Delay(1000);

            await service.StopAsync(cancellationTokenSource.Token);
            Assert.IsFalse(context.QueuedJobs.Any());
        }

        [TestMethod]
        public async Task Saves_Completed_Job_When_Crawler_Completes()
        {
            const int CRAWL_COUNT = 2;
            const string JOB_ID = "job id";
            var DATE_QUEUED = new DateTime(2021, 01, 01);
            var DURATION = TimeSpan.FromSeconds(2);
            var DATA_KEY = "http://localhost/";
            var DATA_VALUE = "data";
            var JOB = new CrawlJob
            {
                Seeds = new List<Uri>
                {
                    new Uri("http://localhost")
                }
            };

            using var context = Setup.CreateContext();
            var logger = new LoggerConfiguration().CreateLogger();

            var mockCrawler = new Mock<ICrawler>();
            mockCrawler.Setup(mock => mock.Crawl(
                    It.IsAny<CrawlJob>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<Action<CrawlResult>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CrawlResult
                {
                    Data = new Dictionary<Uri, IEnumerable<string>>
                    {
                        { new Uri(DATA_KEY), new List<string> { DATA_VALUE } }
                    },
                    CrawlCount = CRAWL_COUNT,
                    Duration = DURATION
                });

            var testNow = new DateTime(2020, 01, 01);
            var nowProvider = new NowProvider(testNow);

            var service = new HostedCrawlerService(context, logger, mockCrawler.Object, nowProvider, _options);

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            context.QueuedJobs.Add(new QueuedJob
            {
                JobJson = JsonConvert.SerializeObject(JOB),
                Id = JOB_ID,
                DateQueued = DATE_QUEUED
            });

            context.SaveChanges();

            await Task.Delay(1000);

            await service.StopAsync(cancellationTokenSource.Token);
            Assert.IsTrue(context.CompletedJobs.Any());

            var completedJob = context.CompletedJobs.First();
            var completedJobData = JsonConvert.DeserializeObject<Dictionary<Uri, IEnumerable<string>>>(completedJob.DataJson);

            Assert.AreEqual(DURATION, completedJob.Duration);
            Assert.AreEqual(CRAWL_COUNT, completedJob.CrawlCount);
            Assert.AreEqual(DATA_KEY, completedJobData.First().Key.AbsoluteUri);
            Assert.AreEqual(DATA_VALUE, completedJobData.First().Value.First());
            Assert.AreEqual(nowProvider.Now, completedJob.DateStarted);
            Assert.AreEqual(nowProvider.Now, completedJob.DateCompleted);
        }

        [TestMethod]
        public async Task Saves_Errored_Job_When_Crawler_Throws()
        {
            const int CRAWL_COUNT = 2;
            const string ERROR_MESSAGE = "error";
            const string JOB_ID = "job id";
            var DATE_QUEUED = new DateTime(2021, 01, 01);
            var DURATION = TimeSpan.FromSeconds(2);
            var DATA_KEY = "http://localhost/";
            var DATA_VALUE = "data";
            var JOB = new CrawlJob
            {
                Seeds = new List<Uri>
                {
                    new Uri("http://localhost")
                }
            };

            using var context = Setup.CreateContext();
            var logger = new LoggerConfiguration().CreateLogger();

            var mockCrawler = new Mock<ICrawler>();
            mockCrawler.Setup(mock => mock.Crawl(
                    It.IsAny<CrawlJob>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<Action<CrawlResult>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new CrawlerRunException(ERROR_MESSAGE, new CrawlResult 
                { 
                    Data = new Dictionary<Uri, IEnumerable<string>>
                    {
                        { new Uri(DATA_KEY), new List<string> { DATA_VALUE } }
                    },
                    CrawlCount = CRAWL_COUNT,
                    Duration = DURATION
                }));

            var testNow = new DateTime(2020, 01, 01);
            var nowProvider = new NowProvider(testNow);

            var service = new HostedCrawlerService(context, logger, mockCrawler.Object, nowProvider, _options);

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            context.QueuedJobs.Add(new QueuedJob
            {
                JobJson = JsonConvert.SerializeObject(JOB),
                Id = JOB_ID,
                DateQueued = DATE_QUEUED
            });

            context.SaveChanges();

            await Task.Delay(2000);

            await service.StopAsync(cancellationTokenSource.Token);
            Assert.IsTrue(context.ErroredJobs.Any());

            var erroredJob = context.ErroredJobs.First();
            var errorData = JsonConvert.DeserializeObject<Dictionary<Uri, IEnumerable<string>>>(erroredJob.DataJson);

            Assert.AreEqual(DURATION, erroredJob.Duration);
            Assert.AreEqual(CRAWL_COUNT, erroredJob.CrawlCount);
            Assert.AreEqual(DATA_KEY, errorData.First().Key.AbsoluteUri);
            Assert.AreEqual(DATA_VALUE, errorData.First().Value.First());
            Assert.AreEqual(nowProvider.Now, erroredJob.DateStarted);
            Assert.AreEqual(nowProvider.Now, erroredJob.DateCompleted);
        }

        [TestMethod]
        public async Task Saves_Running_Job_During_Crawl()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Removes_Running_Job_When_Job_Complete()
        {
            const int CRAWL_COUNT = 2;
            const string JOB_ID = "job id";
            var DATE_QUEUED = new DateTime(2021, 01, 01);
            var DURATION = TimeSpan.FromSeconds(2);
            var DATA_KEY = "http://localhost/";
            var DATA_VALUE = "data";
            var JOB = new CrawlJob
            {
                Seeds = new List<Uri>
                {
                    new Uri("http://localhost")
                }
            };

            using var context = Setup.CreateContext();
            var logger = new LoggerConfiguration().CreateLogger();

            var mockCrawler = new Mock<ICrawler>();
            mockCrawler.Setup(mock => mock.Crawl(
                    It.IsAny<CrawlJob>(), 
                    It.IsAny<TimeSpan>(),
                    It.IsAny<Action<CrawlResult>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CrawlResult
                {
                    Data = new Dictionary<Uri, IEnumerable<string>>
                    {
                        { new Uri(DATA_KEY), new List<string> { DATA_VALUE } }
                    },
                    CrawlCount = CRAWL_COUNT,
                    Duration = DURATION
                });

            var testNow = new DateTime(2020, 01, 01);
            var nowProvider = new NowProvider(testNow);

            var service = new HostedCrawlerService(context, logger, mockCrawler.Object, nowProvider, _options);

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            context.QueuedJobs.Add(new QueuedJob
            {
                JobJson = JsonConvert.SerializeObject(JOB),
                Id = JOB_ID,
                DateQueued = DATE_QUEUED
            });

            context.SaveChanges();

            await Task.Delay(2000);

            await service.StopAsync(cancellationTokenSource.Token);
            Assert.IsFalse(context.RunningJobs.Any());
        }

        [TestMethod]
        public async Task Stops_Crawl_If_Running_Job_Flagged_As_Cancelled()
        {
            const string JOB_ID = "job id";
            var DATE_QUEUED = new DateTime(2021, 01, 01);
            var JOB = new CrawlJob
            {
                Seeds = new List<Uri>
                {
                    new Uri("http://localhost")
                }
            };

            using var context = Setup.CreateContext();
            var logger = new LoggerConfiguration().CreateLogger();

            var testNow = new DateTime(2020, 01, 01);
            var nowProvider = new NowProvider(testNow);
            var fakeCrawler = new FakeCrawler();

            var service = new HostedCrawlerService(context, logger, fakeCrawler, nowProvider, _options);

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            context.QueuedJobs.Add(new QueuedJob
            {
                JobJson = JsonConvert.SerializeObject(JOB),
                Id = JOB_ID,
                DateQueued = DATE_QUEUED
            });

            context.SaveChanges();

            await Task.Delay(2000);

            var running = context.RunningJobs.Find(JOB_ID);

            running.Cancelled = true;

            context.SaveChanges();

            await service.StopAsync(cancellationTokenSource.Token);
        }
    }
}

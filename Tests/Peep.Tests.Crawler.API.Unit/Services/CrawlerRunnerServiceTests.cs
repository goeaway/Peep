﻿//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Moq;
//using Newtonsoft.Json;
//using Peep.Core;
//using Peep.Core.API.Providers;
//using Peep.Exceptions;
//using Peep.Crawler.Application.Options;
//using Peep.Crawler.Application.Providers;
//using Peep.Crawler.Application.Services;
//using Serilog;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Channels;
//using System.Threading.Tasks;

//namespace Peep.Tests.API.Unit.Services
//{
//    [TestClass]
//    [TestCategory("Crawler API - Unit - Crawler Runner Service")]
//    public class CrawlerRunnerServiceTests
//    {
//        private readonly CrawlConfigOptions _options = new CrawlConfigOptions
//        {
//            ProgressUpdateMilliseconds = 100
//        };

//        [TestMethod]
//        public async Task Execute_Completes_When_CancellationToken_Cancelled()
//        {
//            var logger = new LoggerConfiguration().CreateLogger();

//            var mockCrawler = new Mock<ICrawler>();
//            var nowProvider = new NowProvider();

//            var mockTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
//            var mockRunningJobProvider = new Mock<IRunningCrawlJobProvider>();

//            var service = new CrawlerRunnerService(
//                logger,
//                mockCrawler.Object,
//                nowProvider,
//                _options,
//                mockTokenProvider.Object,
//                mockRunningJobProvider.Object);

//            var cancellationTokenSource = new CancellationTokenSource();

//            await service.StartAsync(cancellationTokenSource.Token);

//            await service.StopAsync(cancellationTokenSource.Token);
//            // if the test just goes on forever we consider it failed...
//        }

//        [TestMethod]
//        public async Task Uses_Crawler_When_Job_Found()
//        {
//            const string JOB_ID = "job id";
//            var DATE_QUEUED = new DateTime(2021, 01, 01);
//            var JOB = new StoppableCrawlJob
//            {
//                Seeds = new List<Uri>
//                {
//                    new Uri("http://localhost")
//                }
//            };

//            var logger = new LoggerConfiguration().CreateLogger();

//            var mockCrawler = new Mock<ICrawler>();
//            mockCrawler.Setup(mock => mock.Crawl(
//                    It.IsAny<StoppableCrawlJob>(),
//                    It.IsAny<TimeSpan>(),
//                    It.IsAny<CancellationToken>()))
//                .Returns(Channel.CreateUnbounded<CrawlProgress>().Reader);

//            var mockTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
//            var mockRunningJobProvider = new Mock<IRunningCrawlJobProvider>();

//            var nowProvider = new NowProvider();

//            var service = new CrawlerRunnerService(
//                logger,
//                mockCrawler.Object,
//                nowProvider,
//                _options,
//                mockTokenProvider.Object,
//                mockRunningJobProvider.Object);

//            var cancellationTokenSource = new CancellationTokenSource();

//            await service.StartAsync(cancellationTokenSource.Token);

//            context.QueuedJobs.Add(new QueuedJob
//            {
//                JobJson = JsonConvert.SerializeObject(JOB),
//                Id = JOB_ID,
//                DateQueued = DATE_QUEUED
//            });

//            context.SaveChanges();

//            await Task.Delay(1000);

//            await service.StopAsync(cancellationTokenSource.Token);
//            mockCrawler.Verify(mock => mock.Crawl(
//                    It.IsAny<StoppableCrawlJob>(),
//                    It.IsAny<TimeSpan>(),
//                    It.IsAny<CancellationToken>()), Times.Once());
//        }

//        [TestMethod]
//        public async Task Removes_Queued_Job_From_Db_Upon_Pickup()
//        {
//            const string JOB_ID = "job id";
//            var DATE_QUEUED = new DateTime(2021, 01, 01);
//            var JOB = new StoppableCrawlJob
//            {
//                Seeds = new List<Uri>
//                {
//                    new Uri("http://localhost")
//                }
//            };

//            var logger = new LoggerConfiguration().CreateLogger();

//            var mockCrawler = new Mock<ICrawler>();
//            mockCrawler.Setup(mock => mock.Crawl(
//                    It.IsAny<StoppableCrawlJob>(),
//                    It.IsAny<TimeSpan>(),
//                    It.IsAny<CancellationToken>()))
//                .Returns(Channel.CreateUnbounded<CrawlProgress>().Reader);

//            var mockTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
//            var mockRunningJobProvider = new Mock<IRunningCrawlJobProvider>();

//            var nowProvider = new NowProvider();

//            var service = new CrawlerRunnerService(
//                logger,
//                mockCrawler.Object,
//                nowProvider,
//                _options,
//                mockTokenProvider.Object,
//                mockRunningJobProvider.Object);

//            var cancellationTokenSource = new CancellationTokenSource();

//            await service.StartAsync(cancellationTokenSource.Token);

//            context.QueuedJobs.Add(new QueuedJob
//            {
//                JobJson = JsonConvert.SerializeObject(JOB),
//                Id = JOB_ID,
//                DateQueued = DATE_QUEUED
//            });

//            context.SaveChanges();

//            await Task.Delay(1500);

//            await service.StopAsync(cancellationTokenSource.Token);
//            Assert.IsFalse(context.QueuedJobs.Any());
//        }

//        [TestMethod]
//        public async Task Saves_Completed_Job_When_Crawler_Completes()
//        {
//            const string JOB_ID = "job id";
//            var DATE_QUEUED = new DateTime(2021, 01, 01);
//            var DURATION = TimeSpan.FromSeconds(2);
//            var DATA_KEY = "http://localhost/";
//            var DATA_VALUE = "data";
//            var JOB = new StoppableCrawlJob
//            {
//                Seeds = new List<Uri>
//                {
//                    new Uri("http://localhost")
//                }
//            };

//            var logger = new LoggerConfiguration().CreateLogger();

//            var channel = Channel.CreateUnbounded<CrawlProgress>();
//            var CRAWL_RESULT = new CrawlProgress
//            {
//                Data = new Dictionary<Uri, IEnumerable<string>>
//                {
//                    { new Uri(DATA_KEY), new List<string> { DATA_VALUE } }
//                }
//            };

//            var mockCrawler = new Mock<ICrawler>();
//            mockCrawler.Setup(mock => mock.Crawl(
//                    It.IsAny<StoppableCrawlJob>(),
//                    It.IsAny<TimeSpan>(),
//                    It.IsAny<CancellationToken>()))
//                .Returns(channel.Reader);

//            var mockTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
//            var mockRunningJobProvider = new Mock<IRunningCrawlJobProvider>();
//            mockRunningJobProvider
//                .Setup(mock => mock.GetRunningJob(It.IsAny<string>()))
//                .ReturnsAsync(new RunningJob
//                {
//                    Id = JOB_ID
//                });

//            var testNow = new DateTime(2020, 01, 01);
//            var nowProvider = new NowProvider(testNow);

//            var service = new CrawlerRunnerService(
//                logger,
//                mockCrawler.Object,
//                nowProvider,
//                _options,
//                mockTokenProvider.Object,
//                mockRunningJobProvider.Object);

//            var cancellationTokenSource = new CancellationTokenSource();

//            await service.StartAsync(cancellationTokenSource.Token);

//            context.QueuedJobs.Add(new QueuedJob
//            {
//                JobJson = JsonConvert.SerializeObject(JOB),
//                Id = JOB_ID,
//                DateQueued = DATE_QUEUED
//            });

//            context.SaveChanges();

//            await Task.Delay(1000);

//            await channel.Writer.WriteAsync(CRAWL_RESULT);
//            channel.Writer.Complete();

//            await Task.Delay(1000);

//            await service.StopAsync(cancellationTokenSource.Token);
//            Assert.IsTrue(context.CompletedJobs.Any());

//            var completedJob = context.CompletedJobs.First();
//            var completedJobData = JsonConvert.DeserializeObject<Dictionary<Uri, IEnumerable<string>>>(completedJob.DataJson);

//            Assert.AreEqual(DATA_KEY, completedJobData.First().Key.AbsoluteUri);
//            Assert.AreEqual(DATA_VALUE, completedJobData.First().Value.First());
//            Assert.AreEqual(nowProvider.Now, completedJob.DateStarted);
//            Assert.AreEqual(nowProvider.Now, completedJob.DateCompleted);
//        }

//        [TestMethod]
//        public async Task Saves_Errored_Job_When_Crawler_Throws()
//        {
//            const string ERROR_MESSAGE = "error";
//            const string JOB_ID = "job id";
//            var DATE_QUEUED = new DateTime(2021, 01, 01);
//            var DURATION = TimeSpan.FromSeconds(2);
//            var DATA_KEY = "http://localhost/";
//            var DATA_VALUE = "data";
//            var JOB = new StoppableCrawlJob
//            {
//                Seeds = new List<Uri>
//                {
//                    new Uri("http://localhost")
//                }
//            };

//            var logger = new LoggerConfiguration().CreateLogger();

//            var channel = Channel.CreateUnbounded<CrawlProgress>();
//            var CRAWL_RESULT = new CrawlProgress
//            {
//                Data = new Dictionary<Uri, IEnumerable<string>>
//                {
//                    { new Uri(DATA_KEY), new List<string> { DATA_VALUE } }
//                },
//            };

//            var mockCrawler = new Mock<ICrawler>();
//            mockCrawler.Setup(mock => mock.Crawl(
//                    It.IsAny<StoppableCrawlJob>(),
//                    It.IsAny<TimeSpan>(),
//                    It.IsAny<CancellationToken>()))
//                .Returns(channel);

//            var mockTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
//            var mockRunningJobProvider = new Mock<IRunningCrawlJobProvider>();
//            mockRunningJobProvider
//                .Setup(mock => mock.GetRunningJob(It.IsAny<string>()))
//                .ReturnsAsync(new RunningJob
//                {
//                    Id = JOB_ID
//                });


//            var testNow = new DateTime(2020, 01, 01);
//            var nowProvider = new NowProvider(testNow);

//            var service = new CrawlerRunnerService(
//                logger,
//                mockCrawler.Object,
//                nowProvider,
//                _options,
//                mockTokenProvider.Object,
//                mockRunningJobProvider.Object);

//            var cancellationTokenSource = new CancellationTokenSource();

//            await service.StartAsync(cancellationTokenSource.Token);

//            context.QueuedJobs.Add(new QueuedJob
//            {
//                JobJson = JsonConvert.SerializeObject(JOB),
//                Id = JOB_ID,
//                DateQueued = DATE_QUEUED
//            });

//            context.SaveChanges();

//            await Task.Delay(1000);

//            channel.Writer.Complete(new CrawlerRunException(ERROR_MESSAGE, new CrawlProgress
//            {
//                Data = new Dictionary<Uri, IEnumerable<string>>
//                    {
//                        { new Uri(DATA_KEY), new List<string> { DATA_VALUE } }
//                    },
//            }));

//            await Task.Delay(1000);

//            await service.StopAsync(cancellationTokenSource.Token);
//            Assert.IsTrue(context.ErroredJobs.Any());

//            var erroredJob = context.ErroredJobs.First();
//            var errorData = JsonConvert.DeserializeObject<Dictionary<Uri, IEnumerable<string>>>(erroredJob.DataJson);

//            Assert.AreEqual(DATA_KEY, errorData.First().Key.AbsoluteUri);
//            Assert.AreEqual(DATA_VALUE, errorData.First().Value.First());
//            Assert.AreEqual(nowProvider.Now, erroredJob.DateStarted);
//            Assert.AreEqual(nowProvider.Now, erroredJob.DateCompleted);
//        }

//        [TestMethod]
//        public async Task Removes_Running_Job_When_Job_Complete()
//        {
//            const string JOB_ID = "job id";
//            var DATE_QUEUED = new DateTime(2021, 01, 01);
//            var DURATION = TimeSpan.FromSeconds(2);
//            var DATA_KEY = "http://localhost/";
//            var DATA_VALUE = "data";
//            var JOB = new StoppableCrawlJob
//            {
//                Seeds = new List<Uri>
//                {
//                    new Uri("http://localhost")
//                }
//            };

//            var logger = new LoggerConfiguration().CreateLogger();
//            var channel = Channel.CreateUnbounded<CrawlProgress>();
//            var CRAWL_RESULT = new CrawlProgress
//            {
//                Data = new Dictionary<Uri, IEnumerable<string>>
//                {
//                    { new Uri(DATA_KEY), new List<string> { DATA_VALUE } }
//                },
//            };

//            var mockCrawler = new Mock<ICrawler>();
//            mockCrawler.Setup(mock => mock.Crawl(
//                    It.IsAny<StoppableCrawlJob>(),
//                    It.IsAny<TimeSpan>(),
//                    It.IsAny<CancellationToken>()))
//                .Returns(channel);

//            var mockTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
//            var mockRunningJobProvider = new Mock<IRunningCrawlJobProvider>();
//            mockRunningJobProvider
//                .Setup(mock => mock.GetRunningJob(It.IsAny<string>()))
//                .ReturnsAsync(new RunningJob
//                {
//                    Id = JOB_ID
//                });

//            var testNow = new DateTime(2020, 01, 01);
//            var nowProvider = new NowProvider(testNow);

//            var service = new CrawlerRunnerService(
//                logger,
//                mockCrawler.Object,
//                nowProvider,
//                _options,
//                mockTokenProvider.Object,
//                mockRunningJobProvider.Object);

//            var cancellationTokenSource = new CancellationTokenSource();

//            await service.StartAsync(cancellationTokenSource.Token);

//            context.QueuedJobs.Add(new QueuedJob
//            {
//                JobJson = JsonConvert.SerializeObject(JOB),
//                Id = JOB_ID,
//                DateQueued = DATE_QUEUED
//            });

//            context.SaveChanges();

//            await Task.Delay(1000);

//            await channel.Writer.WriteAsync(CRAWL_RESULT);
//            channel.Writer.Complete();

//            await Task.Delay(1000);

//            await service.StopAsync(cancellationTokenSource.Token);

//            mockRunningJobProvider.Verify(mock => mock.SaveJob(It.IsAny<RunningJob>()), Times.AtLeastOnce());
//            mockRunningJobProvider.Verify(mock => mock.RemoveJob(JOB_ID), Times.Once());
//        }

//        [TestMethod]
//        public async Task Cancels_Crawl_When_Token_Provider_Signals_Job_Cancel()
//        {
//            const int CRAWL_COUNT = 2;
//            const string JOB_ID = "job id";
//            var DATE_QUEUED = new DateTime(2021, 01, 01);
//            var DURATION = TimeSpan.FromSeconds(2);
//            var DATA_KEY = "http://localhost/";
//            var DATA_VALUE = "data";
//            var JOB = new StoppableCrawlJob
//            {
//                Seeds = new List<Uri>
//                {
//                    new Uri("http://localhost")
//                }
//            };

//            var logger = new LoggerConfiguration().CreateLogger();
//            var channel = Channel.CreateUnbounded<CrawlProgress>();
//            var CRAWL_RESULT = new CrawlResult
//            {
//                CrawlCount = CRAWL_COUNT,
//                Data = new Dictionary<Uri, IEnumerable<string>>
//                {
//                    { new Uri(DATA_KEY), new List<string> { DATA_VALUE } }
//                },
//                Duration = DURATION
//            };

//            var mockCrawler = new Mock<ICrawler>();
//            mockCrawler.Setup(mock => mock.Crawl(
//                    It.IsAny<StoppableCrawlJob>(),
//                    It.IsAny<TimeSpan>(),
//                    It.IsAny<CancellationToken>()))
//                .Returns(channel);

//            var mockTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
//            var alreadyCancelledTokenSource = new CancellationTokenSource();
//            alreadyCancelledTokenSource.Cancel();
//            mockTokenProvider.Setup(mock => mock.GetToken(JOB_ID)).Returns(alreadyCancelledTokenSource.Token);

//            var mockRunningJobProvider = new Mock<IRunningCrawlJobProvider>();
//            mockRunningJobProvider
//                .Setup(mock => mock.GetRunningJob(It.IsAny<string>()))
//                .ReturnsAsync(new RunningJob
//                {
//                    Id = JOB_ID
//                });

//            var testNow = new DateTime(2020, 01, 01);
//            var nowProvider = new NowProvider(testNow);

//            var service = new CrawlerRunnerService(
//                logger,
//                mockCrawler.Object,
//                nowProvider,
//                _options,
//                mockTokenProvider.Object,
//                mockRunningJobProvider.Object);

//            var cancellationTokenSource = new CancellationTokenSource();

//            await service.StartAsync(cancellationTokenSource.Token);

//            context.QueuedJobs.Add(new QueuedJob
//            {
//                JobJson = JsonConvert.SerializeObject(JOB),
//                Id = JOB_ID,
//                DateQueued = DATE_QUEUED
//            });

//            context.SaveChanges();

//            await Task.Delay(1000);

//            await service.StopAsync(cancellationTokenSource.Token);

//            Assert.IsTrue(context.CompletedJobs.Any());
//        }
//    }
//}

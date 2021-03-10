using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.API.Application.Managers;
using Peep.API.Application.Requests.Commands.RunCrawl;
using Peep.API.Models.Entities;
using Peep.API.Models.Enums;
using Peep.Core.API.Providers;
using Peep.Core.Infrastructure.Filtering;
using Peep.Core.Infrastructure.Messages;
using Peep.Core.Infrastructure.Queuing;
using Peep.StopConditions;
using Serilog;

namespace Peep.Tests.API.Unit.Commands.RunCrawl
{
    [TestClass]
    [TestCategory("API - Unit - Run Crawl Handler")]
    public class HandlerTests
    {
        [TestMethod]
        public async Task No_Running_Job_Is_Left_After_Completion()
        {
            const string JOB_ID = "id";
            var job = new Job
            {
                Id = JOB_ID
            };

            var jobData = new StoppableCrawlJob();

            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            var crawlerManager = new Mock<ICrawlerManager>();
            
            await using var context = Setup.CreateContext();

            await context.Jobs.AddAsync(job);

            await context.SaveChangesAsync();
                
            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object,
                crawlerManager.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { JobId = JOB_ID, JobActual = jobData }, cancellationTokenSource.Token); 

            Assert.AreEqual(0, context.Jobs.Count(j => j.State == JobState.Running));
        }

        [TestMethod]
        public async Task Seeds_Are_Enqueued()
        {
            const string JOB_ID = "id";

            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var job = new Job
            {
                Id = JOB_ID
            };

            var jobData = new StoppableCrawlJob
            {
                Seeds = seeds
            };
            

            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            var crawlerManager = new Mock<ICrawlerManager>();

            await using var context = Setup.CreateContext();

            await context.Jobs.AddAsync(job);

            await context.SaveChangesAsync();

            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object,
                crawlerManager.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { JobId = JOB_ID, JobActual = jobData }, cancellationTokenSource.Token); 

            queueManager
                .Verify(
                    mock => mock
                        .Enqueue(
                            It.Is<IEnumerable<Uri>>(
                                value => value
                                    .All(v => seeds
                                        .Select(s => s.AbsoluteUri).Contains(v.AbsoluteUri)))),
                    Times.Once());
        }

        [TestMethod]
        public async Task Publishes_CrawlQueued_Message_When_Starting()
        {
            const string JOB_ID = "id";
            
            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var job = new Job
            {
                Id = JOB_ID
            };

            var jobData = new StoppableCrawlJob
            {
                Seeds = seeds
            };

            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            var crawlerManager = new Mock<ICrawlerManager>();
            
            await using var context = Setup.CreateContext();

            await context.AddAsync(job);

            await context.SaveChangesAsync();
            
            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object,
                crawlerManager.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { JobId = JOB_ID, JobActual = jobData }, cancellationTokenSource.Token); 
            
            publishEndpoint
                .Verify(
                    mock => mock
                        .Publish(
                            It.Is<CrawlQueued>(value => value.Job.Id == JOB_ID), 
                            It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [TestMethod]
        public async Task Gets_Filter_Count()
        {
            const string JOB_ID = "id";

            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var job = new Job
            {
                Id = JOB_ID
            };

            var jobData = new StoppableCrawlJob
            {
                Seeds = seeds
            };
            
            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            var crawlerManager = new Mock<ICrawlerManager>();
            
            await using var context = Setup.CreateContext();
            
            await context.Jobs.AddAsync(job);

            await context.SaveChangesAsync();

            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object,
                crawlerManager.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { JobId = JOB_ID, JobActual = jobData }, cancellationTokenSource.Token); 
            
            filterManager
                .Verify(
                    mock => mock.GetCount(),
                Times.AtLeastOnce());
        }
        
        [TestMethod]
        public async Task Publishes_CrawlCancelled_Message_When_Cancelled()
        {
            const string JOB_ID = "id";

            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var job = new Job
            {
                Id = JOB_ID
            };

            var jobData = new StoppableCrawlJob
            {
                Seeds = seeds
            };
            
            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            var crawlerManager = new Mock<ICrawlerManager>();
            
            await using var context = Setup.CreateContext();

            await context.Jobs.AddAsync(job);

            await context.SaveChangesAsync();

            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object,
                crawlerManager.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { JobId = JOB_ID, JobActual = jobData }, cancellationTokenSource.Token); 
            
            publishEndpoint
                .Verify(
                    mock => mock
                        .Publish(
                            It.Is<CrawlCancelled>(value => value.CrawlId == JOB_ID),
                            It.IsAny<CancellationToken>()
                        ),
                Times.Once());
        }

        [TestMethod]
        public async Task StopConditions_Are_Checked()
        {
            const string JOB_ID = "id";

            var mockStopCondition = new Mock<ICrawlStopCondition>();
            mockStopCondition
                .Setup(
                    mock => mock.Stop(It.IsAny<CrawlResult>()))
                .Returns(true);
            
            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var job = new Job
            {
                Id = JOB_ID
            };

            var jobData = new StoppableCrawlJob
            {
                Seeds = seeds,
                StopConditions = new List<ICrawlStopCondition>
                {
                    mockStopCondition.Object
                }
            };

            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            
            var crawlerManager = new Mock<ICrawlerManager>();
            
            await using var context = Setup.CreateContext();
            
            await context.Jobs.AddAsync(job);

            await context.SaveChangesAsync();
            
            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object,
                crawlerManager.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { JobId = JOB_ID, JobActual = jobData }, cancellationTokenSource.Token); 
            
            mockStopCondition
                .Verify(
                    mock => mock.Stop(It.IsAny<CrawlResult>()),
                Times.AtLeastOnce());
        }
        
        [TestMethod]
        public async Task StopCondition_Reached_Publishes_Cancel_Message()
        {
            const string JOB_ID = "id";
            
            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var job = new Job
            {
                Id = JOB_ID
            };

            var jobData = new StoppableCrawlJob
            {
                Seeds = seeds
            };
            
            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            var crawlerManager = new Mock<ICrawlerManager>();
            
            await using var context = Setup.CreateContext();
            
            await context.Jobs.AddAsync(job);

            await context.SaveChangesAsync();
            
            var mockStopCondition = new Mock<ICrawlStopCondition>();
            mockStopCondition
                .Setup(
                    mock => mock.Stop(It.IsAny<CrawlResult>()))
                .Returns(true);
            
            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object,
                crawlerManager.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { JobId = JOB_ID, JobActual = jobData }, cancellationTokenSource.Token); 
            
            publishEndpoint
                .Verify(
                    mock => mock
                        .Publish(
                            It.IsAny<CrawlCancelled>(),
                            It.IsAny<CancellationToken>()),
                    Times.Once());
        }
        
        [TestMethod]
        public async Task Job_Is_Set_To_Completed_Upon_Completion()
        {
            const string JOB_ID = "id";
            const string DATA_URI = "http://localhost/";
            const string DATA_1 = "data"; 
            const string DATA_2 = "data2";
            const string JOB_JSON = "json";
            const int FILTER_COUNT = 1;
            
            var dateQueued = new DateTime(2020, 01, 01);
            var dateStarted = new DateTime(2021, 01, 01);

            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var job = new Job
            {
                Id = JOB_ID,
                DateQueued = dateQueued,
                JobJson = JOB_JSON,
                State = JobState.Queued,
                DateStarted = dateStarted,
                JobData = new List<JobData>
                {
                    new JobData
                    {
                        Source = DATA_URI,
                        Value = DATA_1
                    },
                    new JobData
                    {
                        Source = DATA_URI,
                        Value = DATA_2
                    }
                }
            };

            var mockStopCondition = new Mock<ICrawlStopCondition>();
            mockStopCondition
                .Setup(
                    mock => mock.Stop(It.IsAny<CrawlResult>()))
                .Returns(true);
            
            var jobActual = new StoppableCrawlJob
            {
                Seeds = seeds,
                StopConditions = new List<ICrawlStopCondition>
                {
                    mockStopCondition.Object
                }
            };

            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider(new DateTime(2021, 01, 01));
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            filterManager
                .Setup(
                    mock => mock.GetCount())
                .ReturnsAsync(FILTER_COUNT);
            var queueManager = new Mock<ICrawlQueueManager>();
            
            var crawlerManager = new Mock<ICrawlerManager>();
            
            await using var context = Setup.CreateContext();
            
            await context.Jobs.AddAsync(job);

            await context.SaveChangesAsync();
                
            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object,
                crawlerManager.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { JobId = JOB_ID, JobActual = jobActual }, cancellationTokenSource.Token); 

            var completedJob = context.Jobs.First();
            
            Assert.AreEqual(JOB_ID, completedJob.Id);
            Assert.AreEqual(DATA_URI, completedJob.JobData.First().Source);
            Assert.AreEqual(DATA_URI, completedJob.JobData.Last().Source);
            Assert.AreEqual(DATA_1, completedJob.JobData.First().Value);
            Assert.AreEqual(DATA_2, completedJob.JobData.Last().Value);
            Assert.AreEqual(dateQueued, completedJob.DateQueued);
            Assert.AreEqual(nowProvider.Now, completedJob.DateCompleted);
            Assert.AreEqual(dateStarted, completedJob.DateStarted);
            Assert.AreEqual(JOB_JSON, completedJob.JobJson);
            Assert.AreEqual(0, completedJob.JobErrors.Count());
            Assert.AreEqual(FILTER_COUNT, completedJob.CrawlCount);
            Assert.AreEqual(nowProvider.Now - nowProvider.Now, completedJob.DateCompleted - completedJob.DateStarted);
            Assert.AreEqual(JobState.Complete, completedJob.State);
        }
        
        [TestMethod]
        public async Task Job_Is_Set_To_Cancelled_Upon_Cancellation()
        {
            const string JOB_ID = "id";
            const string DATA_URI = "http://localhost/";
            const string DATA_1 = "data"; 
            const string DATA_2 = "data2";
            const string JOB_JSON = "json";
            const int FILTER_COUNT = 1;
            
            var dateQueued = new DateTime(2020, 01, 01);
            var dateStarted = new DateTime(2021, 01, 01);

            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var job = new Job
            {
                Id = JOB_ID,
                DateQueued = dateQueued,
                JobJson = JOB_JSON,
                State = JobState.Queued,
                DateStarted = dateStarted,
                JobData = new List<JobData>
                {
                    new JobData
                    {
                        Source = DATA_URI,
                        Value = DATA_1
                    },
                    new JobData
                    {
                        Source = DATA_URI,
                        Value = DATA_2
                    }
                }
            };

            var jobActual = new StoppableCrawlJob
            {
                Seeds = seeds
            };

            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider(new DateTime(2021, 01, 01));
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            filterManager
                .Setup(
                    mock => mock.GetCount())
                .ReturnsAsync(FILTER_COUNT);
            var queueManager = new Mock<ICrawlQueueManager>();
            
            var crawlerManager = new Mock<ICrawlerManager>();
            
            await using var context = Setup.CreateContext();
            
            await context.Jobs.AddAsync(job);

            await context.SaveChangesAsync();
                
            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object,
                crawlerManager.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { JobId = JOB_ID, JobActual = jobActual }, cancellationTokenSource.Token); 

            var completedJob = context.Jobs.First();
            
            Assert.AreEqual(JOB_ID, completedJob.Id);
            Assert.AreEqual(DATA_URI, completedJob.JobData.First().Source);
            Assert.AreEqual(DATA_URI, completedJob.JobData.Last().Source);
            Assert.AreEqual(DATA_1, completedJob.JobData.First().Value);
            Assert.AreEqual(DATA_2, completedJob.JobData.Last().Value);
            Assert.AreEqual(dateQueued, completedJob.DateQueued);
            Assert.AreEqual(nowProvider.Now, completedJob.DateCompleted);
            Assert.AreEqual(dateStarted, completedJob.DateStarted);
            Assert.AreEqual(JOB_JSON, completedJob.JobJson);
            Assert.AreEqual(0, completedJob.JobErrors.Count());
            Assert.AreEqual(FILTER_COUNT, completedJob.CrawlCount);
            Assert.AreEqual(nowProvider.Now - nowProvider.Now, completedJob.DateCompleted - completedJob.DateStarted);
            Assert.AreEqual(JobState.Cancelled, completedJob.State);
        }
        
        [TestMethod]
        public async Task Clears_QueueManager()
        {
            const string JOB_ID = "id";

            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var job = new Job 
            {
                Id = JOB_ID
            };

            var jobData = new StoppableCrawlJob
            {
                Seeds = seeds
            };
            
            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            
            var crawlerManager = new Mock<ICrawlerManager>();
            
            await using var context = Setup.CreateContext();

            await context.AddAsync(job);

            await context.SaveChangesAsync();

            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object,
                crawlerManager.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { JobId = JOB_ID, JobActual = jobData }, cancellationTokenSource.Token); 

            queueManager
                .Verify(
                    mock => mock.Clear(),
                    Times.Exactly(2));
        }
        
        [TestMethod]
        public async Task Clears_FilterManager()
        {
            const string JOB_ID = "id";

            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var job = new Job
            {
                Id = JOB_ID
            };

            var jobData = new StoppableCrawlJob
            {
                Seeds = seeds
            };
            
            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            
            var crawlerManager = new Mock<ICrawlerManager>();
            
            await using var context = Setup.CreateContext();
            
            await context.Jobs.AddAsync(job);

            await context.SaveChangesAsync();
                
            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object,
                crawlerManager.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { JobId = JOB_ID, JobActual = jobData }, cancellationTokenSource.Token); 
            
            filterManager
                .Verify(
                    mock => mock.Clear(),
                    Times.Exactly(2));
        }
        
        [TestMethod]
        public async Task Token_Is_Disposed_Of_After_Crawl()
        {
            const string JOB_ID = "id";

            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var job = new Job
            {
                Id = JOB_ID
            };

            var jobData = new StoppableCrawlJob
            {
                Seeds = seeds
            };

            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            var crawlerManager = new Mock<ICrawlerManager>();
            
            await using var context = Setup.CreateContext();
            
            await context.Jobs.AddAsync(job);

            await context.SaveChangesAsync();
                
            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object,
                crawlerManager.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { JobId = JOB_ID, JobActual = jobData }, cancellationTokenSource.Token); 
            
            cancellationTokenProvider
                .Verify(
                    mock => mock.DisposeOfToken(JOB_ID),
                    Times.Once());
        }
        
        [TestMethod]
        public async Task CrawlerManager_Is_Used_To_Wait_For_Crawlers_To_Finish()
        {
            const string JOB_ID = "id";

            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var job = new Job
            {
                Id = JOB_ID
            };

            var jobData = new StoppableCrawlJob
            {
                Seeds = seeds
            };

            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            var crawlerManager = new Mock<ICrawlerManager>();
            
            await using var context = Setup.CreateContext();
                
            await context.Jobs.AddAsync(job);

            await context.SaveChangesAsync();
            
            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object,
                crawlerManager.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { JobId = JOB_ID, JobActual = jobData }, cancellationTokenSource.Token); 
            
            crawlerManager
                .Verify(
                    mock => mock.WaitAllFinished(JOB_ID, It.IsAny<TimeSpan>()),
                    Times.Once());
        }
        
        [TestMethod]
        public async Task CrawlerManager_Adds_Timeout_Error_To_Completed_Job_If_Timeout_Occurred_While_Waiting()
        {
            const string JOB_ID = "id";
            const string TIMEOUT_MESSAGE = "timeout";

            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var job = new Job
            {
                Id = JOB_ID
            };

            var jobData = new StoppableCrawlJob
            {
                Seeds = seeds
            };

            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            var crawlerManager = new Mock<ICrawlerManager>();
            crawlerManager
                .Setup(
                    mock => mock.WaitAllFinished(JOB_ID, It.IsAny<TimeSpan>()))
                .ThrowsAsync(new TimeoutException(TIMEOUT_MESSAGE));
            
            await using var context = Setup.CreateContext();
            
            await context.Jobs.AddAsync(job);

            await context.SaveChangesAsync();
                
            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object,
                crawlerManager.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { JobId = JOB_ID, JobActual = jobData }, cancellationTokenSource.Token); 
            
            // check completed job has something about timeout error
            var completed = context.Jobs.First();
            
            Assert.AreEqual(TIMEOUT_MESSAGE, completed.JobErrors.First().Message);
            Assert.AreEqual("API", completed.JobErrors.First().Source);
        }
    }
}
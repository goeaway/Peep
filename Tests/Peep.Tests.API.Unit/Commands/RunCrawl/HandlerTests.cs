using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.API.Application.Requests.Commands.RunCrawl;
using Peep.API.Models.Entities;
using Peep.Core.API.Providers;
using Peep.Core.Infrastructure.Data;
using Peep.Core.Infrastructure.Filtering;
using Peep.Core.Infrastructure.Messages;
using Peep.Core.Infrastructure.Queuing;
using Peep.Data;
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
            var job = new QueuedJob
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
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            
            await using var context = Setup.CreateContext();

            await context.SaveChangesAsync();
                
            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { Job = job, JobData = jobData }, cancellationTokenSource.Token); 

            Assert.AreEqual(0, context.RunningJobs.Count());
            
        }

        [TestMethod]
        public async Task Seeds_Are_Enqueued()
        {
            const string JOB_ID = "id";

            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var job = new QueuedJob
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
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();

            await using var context = Setup.CreateContext();

            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { Job = job, JobData = jobData }, cancellationTokenSource.Token); 

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
        public async Task CrawlQueued_Message_Is_Published()
        {
            const string JOB_ID = "id";
            
            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var job = new QueuedJob
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
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            
            await using var context = Setup.CreateContext();

            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { Job = job, JobData = jobData }, cancellationTokenSource.Token); 
            
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

            var job = new QueuedJob
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
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            
            await using var context = Setup.CreateContext();

            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { Job = job, JobData = jobData }, cancellationTokenSource.Token); 
            
            filterManager
                .Verify(
                    mock => mock.GetCount(),
                Times.AtLeastOnce());
        }
        
        [TestMethod]
        public async Task Gets_Data_Count()
        {
            const string JOB_ID = "id";

            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var job = new QueuedJob
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
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            
            await using var context = Setup.CreateContext();

            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { Job = job, JobData = jobData }, cancellationTokenSource.Token); 
            
            dataSinkManager
                .Verify(
                    mock => mock.GetCount(JOB_ID),
                    Times.AtLeastOnce());
        }
        
        [TestMethod]
        public async Task CrawlCancelled_Message_Is_Published_When_Cancelled()
        {
            const string JOB_ID = "id";

            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var job = new QueuedJob
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
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            
            await using var context = Setup.CreateContext();

            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { Job = job, JobData = jobData }, cancellationTokenSource.Token); 
            
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

            var job = new QueuedJob
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
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            
            await using var context = Setup.CreateContext();
            
            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { Job = job, JobData = jobData }, cancellationTokenSource.Token); 
            
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

            var job = new QueuedJob
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
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            
            await using var context = Setup.CreateContext();
            
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
                dataSinkManager.Object,
                errorSinkManager.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { Job = job, JobData = jobData }, cancellationTokenSource.Token); 
            
            publishEndpoint
                .Verify(
                    mock => mock
                        .Publish(
                            It.IsAny<CrawlCancelled>(),
                            It.IsAny<CancellationToken>()),
                    Times.Once());
        }
        
        [TestMethod]
        public async Task CompletedJob_Is_Added_Upon_Completion()
        {
            const string JOB_ID = "id";

            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var job = new QueuedJob
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
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            
            await using var context = Setup.CreateContext();
                
            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { Job = job, JobData = jobData }, cancellationTokenSource.Token); 

            Assert.IsTrue(context.CompletedJobs.Any());
            
        }

        [TestMethod]
        public async Task DataManager_Clear_Is_Used()
        {
            const string JOB_ID = "id";

            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var job = new QueuedJob
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
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            
            await using var context = Setup.CreateContext();
                
            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { Job = job, JobData = jobData }, cancellationTokenSource.Token); 
            
            dataSinkManager
                .Verify(
                    mock => mock.Clear(JOB_ID),
                Times.Exactly(2));
        }
        
        [TestMethod]
        public async Task QueueManager_Clear_Is_Used()
        {
            const string JOB_ID = "id";

            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var job = new QueuedJob
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
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            
            await using var context = Setup.CreateContext();

            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { Job = job, JobData = jobData }, cancellationTokenSource.Token); 

            queueManager
                .Verify(
                    mock => mock.Clear(),
                    Times.Exactly(2));
        }
        
        [TestMethod]
        public async Task FilterManager_Clear_Is_Used()
        {
            const string JOB_ID = "id";

            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var job = new QueuedJob
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
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            
            await using var context = Setup.CreateContext();
                
            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { Job = job, JobData = jobData }, cancellationTokenSource.Token); 
            
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

            var job = new QueuedJob
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
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            
            await using var context = Setup.CreateContext();
                
            var handler = new RunCrawlHandler(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                filterManager.Object,
                queueManager.Object,
                publishEndpoint.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(500);
            await handler.Handle(new RunCrawlRequest { Job = job, JobData = jobData }, cancellationTokenSource.Token); 
            
            cancellationTokenProvider
                .Verify(
                    mock => mock.DisposeOfToken(JOB_ID),
                    Times.Once());
        }
    }
}
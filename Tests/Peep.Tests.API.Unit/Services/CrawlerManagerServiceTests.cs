using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Moq;
using Newtonsoft.Json;
using Peep.API.Application.Providers;
using Peep.API.Application.Services;
using Peep.API.Models.Entities;
using Peep.Core.API.Providers;
using Peep.Core.Infrastructure.Data;
using Peep.Core.Infrastructure.Filtering;
using Peep.Core.Infrastructure.Messages;
using Peep.Core.Infrastructure.Queuing;
using Peep.Data;
using Peep.StopConditions;
using Serilog;

namespace Peep.Tests.API.Unit.Services
{
    [TestClass]
    [TestCategory("API - Unit - Crawler Manager Service")]
    public class HostedCrawlerServiceTests
    {
        [TestMethod]
        public async Task Dequeues_Queued_Job_When_Found()
        {
            const string JOB_ID = "id";

            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            var jobProvider = new Mock<IQueuedJobProvider>();
            
            await using var context = Setup.CreateContext();

            var outedQueuedJob = new QueuedJob
            {
                Id = JOB_ID,
            };
            
            var outedStoppableJob = new StoppableCrawlJob(); 
            
            jobProvider
                .Setup(
                    mock =>
                        mock.TryGetJob(
                            out outedQueuedJob,
                            out outedStoppableJob))
                .Returns(true);
                
            var service = new CrawlerManagerService(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                publishEndpoint.Object,
                filterManager.Object,
                queueManager.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                jobProvider.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await service.StopAsync(cancellationTokenSource.Token);

            Assert.AreEqual(0, context.QueuedJobs.Count());
        }


        [TestMethod]
        public async Task No_Running_Job_Is_Left_After_Completion()
        {
            const string JOB_ID = "id";

            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            var jobProvider = new Mock<IQueuedJobProvider>();
            
            await using var context = Setup.CreateContext();

            await context.QueuedJobs.AddAsync(new QueuedJob
            {
                Id = JOB_ID
            });

            await context.SaveChangesAsync();
                
            var service = new CrawlerManagerService(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                publishEndpoint.Object,
                filterManager.Object,
                queueManager.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                jobProvider.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await service.StopAsync(cancellationTokenSource.Token);

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

            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            var jobProvider = new Mock<IQueuedJobProvider>();
            
            await using var context = Setup.CreateContext();

            var outedQueuedJob = new QueuedJob
            {
                Id = JOB_ID,
            };
            
            var outedStoppableJob = new StoppableCrawlJob
            {
                Seeds = seeds
            }; 
            
            jobProvider
                .Setup(
                    mock =>
                        mock.TryGetJob(
                            out outedQueuedJob,
                            out outedStoppableJob))
                .Returns(true);
                
            var service = new CrawlerManagerService(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                publishEndpoint.Object,
                filterManager.Object,
                queueManager.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                jobProvider.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await service.StopAsync(cancellationTokenSource.Token);

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

            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            var jobProvider = new Mock<IQueuedJobProvider>();
            
            await using var context = Setup.CreateContext();

            var outedQueuedJob = new QueuedJob
            {
                Id = JOB_ID,
            };
            
            var outedStoppableJob = new StoppableCrawlJob
            {
                Seeds = seeds
            }; 
            
            jobProvider
                .Setup(
                    mock =>
                        mock.TryGetJob(
                            out outedQueuedJob,
                            out outedStoppableJob))
                .Returns(true);
                
            var service = new CrawlerManagerService(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                publishEndpoint.Object,
                filterManager.Object,
                queueManager.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                jobProvider.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await service.StopAsync(cancellationTokenSource.Token);
            
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

            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            var jobProvider = new Mock<IQueuedJobProvider>();
            
            await using var context = Setup.CreateContext();

            var outedQueuedJob = new QueuedJob
            {
                Id = JOB_ID,
            };
            
            var outedStoppableJob = new StoppableCrawlJob
            {
                Seeds = seeds
            }; 
            
            jobProvider
                .Setup(
                    mock =>
                        mock.TryGetJob(
                            out outedQueuedJob,
                            out outedStoppableJob))
                .Returns(true);
                
            var service = new CrawlerManagerService(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                publishEndpoint.Object,
                filterManager.Object,
                queueManager.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                jobProvider.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await service.StopAsync(cancellationTokenSource.Token);
            
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

            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            var jobProvider = new Mock<IQueuedJobProvider>();
            
            await using var context = Setup.CreateContext();

            var outedQueuedJob = new QueuedJob
            {
                Id = JOB_ID,
            };
            
            var outedStoppableJob = new StoppableCrawlJob
            {
                Seeds = seeds
            }; 
            
            jobProvider
                .Setup(
                    mock =>
                        mock.TryGetJob(
                            out outedQueuedJob,
                            out outedStoppableJob))
                .Returns(true);
                
            var service = new CrawlerManagerService(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                publishEndpoint.Object,
                filterManager.Object,
                queueManager.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                jobProvider.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await service.StopAsync(cancellationTokenSource.Token);
            
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

            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            var jobProvider = new Mock<IQueuedJobProvider>();
            
            await using var context = Setup.CreateContext();

            var outedQueuedJob = new QueuedJob
            {
                Id = JOB_ID,
            };
            
            var outedStoppableJob = new StoppableCrawlJob
            {
                Seeds = seeds
            }; 
            
            jobProvider
                .Setup(
                    mock =>
                        mock.TryGetJob(
                            out outedQueuedJob,
                            out outedStoppableJob))
                .Returns(true);
                
            var service = new CrawlerManagerService(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                publishEndpoint.Object,
                filterManager.Object,
                queueManager.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                jobProvider.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);
            cancellationTokenSource.Cancel();

            publishEndpoint
                .Verify(
                    mock => mock
                        .Publish(
                            It.Is<CrawlCancelled>(value => value.CrawlId == JOB_ID),
                            It.IsAny<CancellationToken>()
                        ),
                Times.Once());

            await service.StopAsync(cancellationTokenSource.Token);
        }

        [TestMethod]
        public async Task StopConditions_Are_Checked()
        {
            const string JOB_ID = "id";

            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            var jobProvider = new Mock<IQueuedJobProvider>();
            
            await using var context = Setup.CreateContext();

            var outedQueuedJob = new QueuedJob
            {
                Id = JOB_ID,
            };
            
            var mockStopCondition = new Mock<ICrawlStopCondition>();
            mockStopCondition
                .Setup(
                    mock => mock.Stop(It.IsAny<CrawlResult>()))
                .Returns(true);
            
            var outedStoppableJob = new StoppableCrawlJob
            {
                Seeds = seeds,
                StopConditions = new List<ICrawlStopCondition>
                {
                    mockStopCondition.Object
                }
            }; 
            
            jobProvider
                .SetupSequence(
                    mock =>
                        mock.TryGetJob(
                            out outedQueuedJob,
                            out outedStoppableJob))
                .Returns(true)
                .Returns(false);
                
            var service = new CrawlerManagerService(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                publishEndpoint.Object,
                filterManager.Object,
                queueManager.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                jobProvider.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);
            cancellationTokenSource.Cancel();

            await service.StopAsync(cancellationTokenSource.Token);
            
            mockStopCondition
                .Verify(
                    mock => mock.Stop(It.IsAny<CrawlResult>()),
                Times.AtLeastOnce());
        }
        
        [TestMethod]
        public async Task CompletedJob_Is_Added_Upon_Completion()
        {
            const string JOB_ID = "id";

            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            var jobProvider = new Mock<IQueuedJobProvider>();
            
            await using var context = Setup.CreateContext();

            var outedQueuedJob = new QueuedJob
            {
                Id = JOB_ID,
            };
            
            var outedStoppableJob = new StoppableCrawlJob
            {
                Seeds = seeds
            }; 
            
            jobProvider
                .Setup(
                    mock =>
                        mock.TryGetJob(
                            out outedQueuedJob,
                            out outedStoppableJob))
                .Returns(true);
                
            var service = new CrawlerManagerService(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                publishEndpoint.Object,
                filterManager.Object,
                queueManager.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                jobProvider.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await service.StopAsync(cancellationTokenSource.Token);
            
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

            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            var jobProvider = new Mock<IQueuedJobProvider>();
            
            await using var context = Setup.CreateContext();

            var outedQueuedJob = new QueuedJob
            {
                Id = JOB_ID,
            };
            
            var outedStoppableJob = new StoppableCrawlJob
            {
                Seeds = seeds
            }; 
            
            jobProvider
                .Setup(
                    mock =>
                        mock.TryGetJob(
                            out outedQueuedJob,
                            out outedStoppableJob))
                .Returns(true);
                
            var service = new CrawlerManagerService(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                publishEndpoint.Object,
                filterManager.Object,
                queueManager.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                jobProvider.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await service.StopAsync(cancellationTokenSource.Token);
            
            dataSinkManager
                .Verify(
                    mock => mock.Clear(JOB_ID),
                Times.Once());
        }
        
        [TestMethod]
        public async Task QueueManager_Clear_Is_Used()
        {
            const string JOB_ID = "id";

            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            var jobProvider = new Mock<IQueuedJobProvider>();
            
            await using var context = Setup.CreateContext();

            var outedQueuedJob = new QueuedJob
            {
                Id = JOB_ID,
            };
            
            var outedStoppableJob = new StoppableCrawlJob
            {
                Seeds = seeds
            }; 
            
            jobProvider
                .Setup(
                    mock =>
                        mock.TryGetJob(
                            out outedQueuedJob,
                            out outedStoppableJob))
                .Returns(true);
                
            var service = new CrawlerManagerService(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                publishEndpoint.Object,
                filterManager.Object,
                queueManager.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                jobProvider.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await service.StopAsync(cancellationTokenSource.Token);
            
            queueManager
                .Verify(
                    mock => mock.Clear(),
                    Times.Once());
        }
        
        [TestMethod]
        public async Task FilterManager_Clear_Is_Used()
        {
            const string JOB_ID = "id";

            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            var jobProvider = new Mock<IQueuedJobProvider>();
            
            await using var context = Setup.CreateContext();

            var outedQueuedJob = new QueuedJob
            {
                Id = JOB_ID,
            };
            
            var outedStoppableJob = new StoppableCrawlJob
            {
                Seeds = seeds
            }; 
            
            jobProvider
                .Setup(
                    mock =>
                        mock.TryGetJob(
                            out outedQueuedJob,
                            out outedStoppableJob))
                .Returns(true);
                
            var service = new CrawlerManagerService(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                publishEndpoint.Object,
                filterManager.Object,
                queueManager.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                jobProvider.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await service.StopAsync(cancellationTokenSource.Token);
            
            filterManager
                .Verify(
                    mock => mock.Clear(),
                    Times.Once());
        }
        
        [TestMethod]
        public async Task Token_Is_Disposed_Of_After_Crawl()
        {
            const string JOB_ID = "id";

            var seeds = new List<Uri>
            {
                new Uri("http://localhost")
            };

            var logger = new LoggerConfiguration().CreateLogger();
            var nowProvider = new NowProvider();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            var filterManager = new Mock<ICrawlFilterManager>();
            var queueManager = new Mock<ICrawlQueueManager>();
            var dataSinkManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();
            var errorSinkManager = new Mock<ICrawlDataSinkManager<CrawlErrors>>();
            var jobProvider = new Mock<IQueuedJobProvider>();
            
            await using var context = Setup.CreateContext();

            var outedQueuedJob = new QueuedJob
            {
                Id = JOB_ID,
            };
            
            var outedStoppableJob = new StoppableCrawlJob
            {
                Seeds = seeds
            }; 
            
            jobProvider
                .Setup(
                    mock =>
                        mock.TryGetJob(
                            out outedQueuedJob,
                            out outedStoppableJob))
                .Returns(true);
                
            var service = new CrawlerManagerService(
                context,
                logger,
                nowProvider,
                cancellationTokenProvider.Object,
                publishEndpoint.Object,
                filterManager.Object,
                queueManager.Object,
                dataSinkManager.Object,
                errorSinkManager.Object,
                jobProvider.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await service.StopAsync(cancellationTokenSource.Token);
            
            cancellationTokenProvider
                .Verify(
                    mock => mock.DisposeOfToken(JOB_ID),
                    Times.Once());
        }
    }
}

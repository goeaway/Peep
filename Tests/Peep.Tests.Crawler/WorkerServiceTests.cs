using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using MassTransit;
using MediatR;
using Peep.Core;
using Peep.Core.API;
using Peep.Core.API.Options;
using Peep.Core.Infrastructure;
using Peep.Core.Infrastructure.Messages;
using Peep.Crawler;
using Peep.Crawler.Application.Providers;
using Peep.Crawler.Application.Requests.Commands.RunCrawl;
using Peep.Crawler.Application.Services;
using Serilog;

namespace Peep.Tests.Crawler
{
    [TestCategory("Crawler - Worker Service")]
    [TestClass]
    public class WorkerServiceTests
    {
        private void SetupEndpointConventions()
        {
            EndpointConvention.Map<CrawlerUp>(new Uri("queue:crawler-up"));
            EndpointConvention.Map<CrawlerDown>(new Uri("queue:crawler-down"));
            EndpointConvention.Map<CrawlerJoined>(new Uri("queue:crawler-joined"));
            EndpointConvention.Map<CrawlerLeft>(new Uri("queue:crawler-left"));
        }
        
        [TestMethod]
        public async Task Checks_Job_Queue_For_Job()
        {
            SetupEndpointConventions();
            
            var logger = new LoggerConfiguration().CreateLogger();
            var jobQueue = new Mock<IJobQueue>();
            var mediator = new Mock<IMediator>();
            var crawlerIdProvider = new Mock<ICrawlerIdProvider>();
            var monitoringOptions = new MonitoringOptions
            {
                MaxUnresponsiveTicks = 3,
                TickSeconds = 1
            };
            var sendEndpoint = new Mock<ISendEndpoint>();
            var sendEndpointProvider = new Mock<ISendEndpointProvider>();
            sendEndpointProvider
                .Setup(mock => mock.GetSendEndpoint(It.IsAny<Uri>()))
                .ReturnsAsync(sendEndpoint.Object);
            
            var service = new Worker(
                    logger,
                    jobQueue.Object,
                    mediator.Object,
                    crawlerIdProvider.Object,
                    sendEndpointProvider.Object,
                    monitoringOptions
                );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await Task.Delay(200);

            await service.StopAsync(cancellationTokenSource.Token);
            
            jobQueue.Verify(
                mock => mock.TryDequeue(out It.Ref<IdentifiableCrawlJob>.IsAny), 
                Times.AtLeast(1));
        }

        [TestMethod]
        public async Task Calls_Mediator_With_RunCrawlRequest_And_Job()
        {
            var passedJob = new IdentifiableCrawlJob();
            
            var logger = new LoggerConfiguration().CreateLogger();
            var jobQueue = new Mock<IJobQueue>();
            jobQueue
                .SetupSequence(
                    mock => mock.TryDequeue(out passedJob))
                .Returns(true)
                .Returns(false);
            var crawlerIdProvider = new Mock<ICrawlerIdProvider>();
            var monitoringOptions = new MonitoringOptions
            {
                MaxUnresponsiveTicks = 3,
                TickSeconds = 1
            };
            var sendEndpoint = new Mock<ISendEndpoint>();
            var sendEndpointProvider = new Mock<ISendEndpointProvider>();
            sendEndpointProvider
                .Setup(mock => mock.GetSendEndpoint(It.IsAny<Uri>()))
                .ReturnsAsync(sendEndpoint.Object);
            
            var mediator = new Mock<IMediator>();
            mediator
                .Setup(
                    mock => mock
                        .Send(
                            It.IsAny<IRequest>(),
                            It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);
            
            var service = new Worker(
                logger,
                jobQueue.Object,
                mediator.Object,
                crawlerIdProvider.Object,
                sendEndpointProvider.Object,
                monitoringOptions
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await Task.Delay(200);

            await service.StopAsync(cancellationTokenSource.Token);
            
            mediator.Verify(
                mock => mock
                    .Send(
                        It.Is<RunCrawlRequest>(value => value.Job == passedJob), 
                        It.IsAny<CancellationToken>()), 
                Times.AtLeast(1));
        }
        
        [TestMethod]
        public async Task Sends_CrawlerJoined_Message_Before_Crawling()
        {
            var passedJob = new IdentifiableCrawlJob();
            
            var logger = new LoggerConfiguration().CreateLogger();
            var jobQueue = new Mock<IJobQueue>();
            jobQueue
                .SetupSequence(
                    mock => mock.TryDequeue(out passedJob))
                .Returns(true)
                .Returns(false);
            var monitoringOptions = new MonitoringOptions
            {
                MaxUnresponsiveTicks = 3,
                TickSeconds = 1
            };
            var crawlerIdProvider = new Mock<ICrawlerIdProvider>();
            crawlerIdProvider
                .Setup(mock => mock.GetCrawlerId())
                .Returns(Environment.MachineName);
            var sendEndpoint = new Mock<ISendEndpoint>();
            var sendEndpointProvider = new Mock<ISendEndpointProvider>();
            sendEndpointProvider
                .Setup(mock => mock.GetSendEndpoint(It.IsAny<Uri>()))
                .ReturnsAsync(sendEndpoint.Object);
            
            var mediator = new Mock<IMediator>();
            mediator
                .Setup(
                    mock => mock
                        .Send(
                            It.IsAny<IRequest>(),
                            It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);
            
            var service = new Worker(
                logger,
                jobQueue.Object,
                mediator.Object,
                crawlerIdProvider.Object,
                sendEndpointProvider.Object,
                monitoringOptions
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await Task.Delay(200);

            await service.StopAsync(cancellationTokenSource.Token);
            
            sendEndpoint
                .Verify(
                    mock => mock
                        .Send(
                            It.Is<CrawlerJoined>(value => value.CrawlerId.Value == Environment.MachineName && value.JobId == passedJob.Id),
                            It.IsAny<CancellationToken>()),
                    Times.Once());
        }
        
        [TestMethod]
        public async Task Sends_CrawlerLeft_Message_After_Crawling()
        {
            var passedJob = new IdentifiableCrawlJob();
            
            var logger = new LoggerConfiguration().CreateLogger();
            var jobQueue = new Mock<IJobQueue>();
            jobQueue
                .SetupSequence(
                    mock => mock.TryDequeue(out passedJob))
                .Returns(true)
                .Returns(false);
            var monitoringOptions = new MonitoringOptions
            {
                MaxUnresponsiveTicks = 3,
                TickSeconds = 1
            };
            var crawlerIdProvider = new Mock<ICrawlerIdProvider>();
            crawlerIdProvider
                .Setup(mock => mock.GetCrawlerId())
                .Returns(Environment.MachineName);
            var sendEndpoint = new Mock<ISendEndpoint>();
            var sendEndpointProvider = new Mock<ISendEndpointProvider>();
            sendEndpointProvider
                .Setup(mock => mock.GetSendEndpoint(It.IsAny<Uri>()))
                .ReturnsAsync(sendEndpoint.Object);
            
            var mediator = new Mock<IMediator>();
            mediator
                .Setup(
                    mock => mock
                        .Send(
                            It.IsAny<IRequest>(),
                            It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);
            
            var service = new Worker(
                logger,
                jobQueue.Object,
                mediator.Object,
                crawlerIdProvider.Object,
                sendEndpointProvider.Object,
                monitoringOptions
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await Task.Delay(200);

            await service.StopAsync(cancellationTokenSource.Token);
            
            sendEndpoint
                .Verify(
                    mock => mock
                        .Send(
                            It.Is<CrawlerLeft>(value => value.CrawlerId.Value == Environment.MachineName && value.JobId == passedJob.Id),
                            It.IsAny<CancellationToken>()),
                    Times.Once());
        }
        
        [TestMethod]
        public async Task Sends_CrawlerUp_Message()
        {
            var passedJob = new IdentifiableCrawlJob();
            
            var logger = new LoggerConfiguration().CreateLogger();
            var jobQueue = new Mock<IJobQueue>();
            jobQueue
                .Setup(
                    mock => mock.TryDequeue(out passedJob))
                .Returns(false);
            var monitoringOptions = new MonitoringOptions
            {
                MaxUnresponsiveTicks = 3,
                TickSeconds = 1
            };
            var crawlerIdProvider = new Mock<ICrawlerIdProvider>();
            crawlerIdProvider
                .Setup(mock => mock.GetCrawlerId())
                .Returns(Environment.MachineName);
            var sendEndpoint = new Mock<ISendEndpoint>();
            var sendEndpointProvider = new Mock<ISendEndpointProvider>();
            sendEndpointProvider
                .Setup(mock => mock.GetSendEndpoint(It.IsAny<Uri>()))
                .ReturnsAsync(sendEndpoint.Object);
            
            var mediator = new Mock<IMediator>();
            
            var service = new Worker(
                logger,
                jobQueue.Object,
                mediator.Object,
                crawlerIdProvider.Object,
                sendEndpointProvider.Object,
                monitoringOptions
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await Task.Delay(200);

            await service.StopAsync(cancellationTokenSource.Token);
            
            sendEndpoint
                .Verify(
                    mock => mock
                        .Send(
                            It.Is<CrawlerUp>(value => value.CrawlerId.Value == Environment.MachineName),
                            It.IsAny<CancellationToken>()),
                    Times.Once());
        }
        
        [TestMethod]
        public async Task Sends_CrawlerDown_Message()
        {
            var passedJob = new IdentifiableCrawlJob();
            
            var logger = new LoggerConfiguration().CreateLogger();
            var jobQueue = new Mock<IJobQueue>();
            jobQueue
                .Setup(
                    mock => mock.TryDequeue(out passedJob))
                .Returns(false);
            var monitoringOptions = new MonitoringOptions
            {
                MaxUnresponsiveTicks = 3,
                TickSeconds = 1
            };
            var crawlerIdProvider = new Mock<ICrawlerIdProvider>();
            crawlerIdProvider
                .Setup(mock => mock.GetCrawlerId())
                .Returns(Environment.MachineName);
            var sendEndpoint = new Mock<ISendEndpoint>();
            var sendEndpointProvider = new Mock<ISendEndpointProvider>();
            sendEndpointProvider
                .Setup(mock => mock.GetSendEndpoint(It.IsAny<Uri>()))
                .ReturnsAsync(sendEndpoint.Object);
            
            var mediator = new Mock<IMediator>();
            
            var service = new Worker(
                logger,
                jobQueue.Object,
                mediator.Object,
                crawlerIdProvider.Object,
                sendEndpointProvider.Object,
                monitoringOptions
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await Task.Delay(200);

            await service.StopAsync(cancellationTokenSource.Token);
            
            sendEndpoint
                .Verify(
                    mock => mock
                        .Send(
                            It.Is<CrawlerDown>(value => value.CrawlerId.Value == Environment.MachineName),
                            It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [TestMethod]
        public async Task Sends_CrawlerHeartbeat_While_Crawler_Is_Running()
        {
            var passedJob = new IdentifiableCrawlJob();
            
            var logger = new LoggerConfiguration().CreateLogger();
            var jobQueue = new Mock<IJobQueue>();
            jobQueue
                .SetupSequence(
                    mock => mock.TryDequeue(out passedJob))
                .Returns(true)
                .Returns(false);
            var crawlerIdProvider = new CrawlerIdProvider(CrawlerId.FromMachineName());
            var monitoringOptions = new MonitoringOptions
            {
                MaxUnresponsiveTicks = 3,
                TickSeconds = 1
            };
            var sendEndpoint = new Mock<ISendEndpoint>();
            var sendEndpointProvider = new Mock<ISendEndpointProvider>();
            sendEndpointProvider
                .Setup(mock => mock.GetSendEndpoint(It.IsAny<Uri>()))
                .ReturnsAsync(sendEndpoint.Object);
            
            var mediator = new Mock<IMediator>();
            mediator
                .Setup(
                    mock => mock
                        .Send(
                            It.IsAny<RunCrawlRequest>(),
                            It.IsAny<CancellationToken>()))
                .Returns(Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    return new Either<Unit, HttpErrorResponse>(Unit.Value);
                }));
            
            var service = new Worker(
                logger,
                jobQueue.Object,
                mediator.Object,
                crawlerIdProvider,
                sendEndpointProvider.Object,
                monitoringOptions
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await Task.Delay(3000);

            await service.StopAsync(cancellationTokenSource.Token);
            
            sendEndpoint
                .Verify(
                    mock => mock
                        .Send(
                            It.Is<CrawlerHeartbeat>(
                                value => value.CrawlerId.Value == Environment.MachineName),
                            It.IsAny<CancellationToken>()),
                    Times.AtLeastOnce());
        }
        
        [TestMethod]
        public async Task Sends_CrawlerHeartbeat_While_Crawler_Is_Not_Running()
        {
            var passedJob = new IdentifiableCrawlJob();
            
            var logger = new LoggerConfiguration().CreateLogger();
            var jobQueue = new Mock<IJobQueue>();
            jobQueue
                .Setup(
                    mock => mock.TryDequeue(out passedJob))
                .Returns(false);
            
            var crawlerIdProvider = new CrawlerIdProvider(CrawlerId.FromMachineName());
            var monitoringOptions = new MonitoringOptions
            {
                MaxUnresponsiveTicks = 3,
                TickSeconds = 1
            };
            
            var sendEndpoint = new Mock<ISendEndpoint>();
            var sendEndpointProvider = new Mock<ISendEndpointProvider>();
            sendEndpointProvider
                .Setup(mock => mock.GetSendEndpoint(It.IsAny<Uri>()))
                .ReturnsAsync(sendEndpoint.Object);
            
            var mediator = new Mock<IMediator>();

            var service = new Worker(
                logger,
                jobQueue.Object,
                mediator.Object,
                crawlerIdProvider,
                sendEndpointProvider.Object,
                monitoringOptions
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await Task.Delay(3000);

            await service.StopAsync(cancellationTokenSource.Token);
            
            sendEndpoint
                .Verify(
                    mock => mock
                        .Send(
                            It.Is<CrawlerHeartbeat>(
                                value => value.CrawlerId.Value == Environment.MachineName),
                            It.IsAny<CancellationToken>()),
                    Times.AtLeastOnce());
        }
    }
}

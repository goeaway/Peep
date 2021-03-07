using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using MassTransit;
using MediatR;
using Peep.Core.Infrastructure.Messages;
using Peep.Crawler;
using Peep.Crawler.Application.Requests.Commands.RunCrawl;
using Peep.Crawler.Application.Services;
using Serilog;

namespace Peep.Tests.Crawler
{
    [TestCategory("Crawler - Worker Service")]
    [TestClass]
    public class WorkerServiceTests
    {
        [TestMethod]
        public async Task Checks_Job_Queue_For_Job()
        {
            var logger = new LoggerConfiguration().CreateLogger();
            var jobQueue = new Mock<IJobQueue>();
            var mediator = new Mock<IMediator>();
            var publishEndpoint = new Mock<IPublishEndpoint>();
            
            var service = new Worker(
                    logger,
                    jobQueue.Object,
                    mediator.Object,
                    publishEndpoint.Object
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
            var publishEndpoint = new Mock<IPublishEndpoint>();
            
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
                publishEndpoint.Object
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
        public async Task Sends_CrawlerStarted_Message_Before_Crawling()
        {
            var passedJob = new IdentifiableCrawlJob();
            
            var logger = new LoggerConfiguration().CreateLogger();
            var jobQueue = new Mock<IJobQueue>();
            jobQueue
                .SetupSequence(
                    mock => mock.TryDequeue(out passedJob))
                .Returns(true)
                .Returns(false);
            var publishEndpoint = new Mock<IPublishEndpoint>();
            
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
                publishEndpoint.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await Task.Delay(200);

            await service.StopAsync(cancellationTokenSource.Token);
            
            publishEndpoint
                .Verify(
                    mock => mock
                        .Publish(
                            It.Is<CrawlerStarted>(value => value.CrawlerId == Environment.MachineName && value.JobId == passedJob.Id),
                            It.IsAny<CancellationToken>()),
                    Times.Once());
        }
        
        [TestMethod]
        public async Task Sends_CrawlerFinished_Message_After_Crawling()
        {
            var passedJob = new IdentifiableCrawlJob();
            
            var logger = new LoggerConfiguration().CreateLogger();
            var jobQueue = new Mock<IJobQueue>();
            jobQueue
                .SetupSequence(
                    mock => mock.TryDequeue(out passedJob))
                .Returns(true)
                .Returns(false);
            var publishEndpoint = new Mock<IPublishEndpoint>();
            
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
                publishEndpoint.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await Task.Delay(200);

            await service.StopAsync(cancellationTokenSource.Token);
            
            publishEndpoint
                .Verify(
                    mock => mock
                        .Publish(
                            It.Is<CrawlerFinished>(value => value.CrawlerId == Environment.MachineName && value.JobId == passedJob.Id),
                            It.IsAny<CancellationToken>()),
                    Times.Once());
        }
    }
}

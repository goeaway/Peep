using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using System.Threading.Channels;
using MediatR;
using Peep.Core.API.Providers;
using Peep.Core.Infrastructure.Data;
using Peep.Crawler;
using Peep.Crawler.Application.Options;
using Peep.Crawler.Application.Requests.Commands.RunCrawl;
using Peep.Crawler.Application.Services;
using Peep.Data;
using Peep.Exceptions;
using Peep.Filtering;
using Peep.Queueing;
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
            
            var service = new Worker(
                    logger,
                    jobQueue.Object,
                    mediator.Object
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
            var PASSED_JOB = new IdentifiableCrawlJob();
            
            var logger = new LoggerConfiguration().CreateLogger();
            var jobQueue = new Mock<IJobQueue>();
            jobQueue
                .SetupSequence(
                    mock => mock.TryDequeue(out PASSED_JOB))
                .Returns(true)
                .Returns(false);
            
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
                mediator.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await Task.Delay(200);

            await service.StopAsync(cancellationTokenSource.Token);
            
            mediator.Verify(
                mock => mock
                    .Send(
                        It.Is<RunCrawlRequest>(value => value.Job == PASSED_JOB), 
                        It.IsAny<CancellationToken>()), 
                Times.AtLeast(1));
        }
    }
}

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Moq;
using Newtonsoft.Json;
using Peep.API.Application.Requests.Commands.RunCrawl;
using Peep.API.Application.Services;
using Peep.API.Models.Entities;
using Peep.API.Models.Enums;
using Peep.Core.API.Providers;
using Serilog;

namespace Peep.Tests.API.Unit.Services
{
    [TestClass]
    [TestCategory("API - Unit - Crawler Runner Service")]
    public class CrawlerRunnerServiceTests
    {
        [TestMethod]
        public async Task Queued_Job_Changes_To_Running_Job_When_Found()
        {
            const string JOB_ID = "id";
            
            var logger = new LoggerConfiguration().CreateLogger();
            var mediator = new Mock<IMediator>();
            var now = new DateTime(2020, 01, 01);
            var nowProvider = new NowProvider(now);
            
            await using var context = Setup.CreateContext();

            await context.Jobs.AddAsync(new Job
            {
                Id = JOB_ID,
                JobJson = JsonConvert.SerializeObject(new StoppableCrawlJob())
            });

            await context.SaveChangesAsync();
            
            var service = new CrawlRunnerService(
                logger,
                mediator.Object,
                context,
                nowProvider
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await service.StopAsync(cancellationTokenSource.Token);

            var queuedJob = context.Jobs.First();
            Assert.AreEqual(JobState.Running, queuedJob.State);
            Assert.AreEqual(now, queuedJob.DateStarted);
        }

        [TestMethod]
        public async Task Uses_Mediator_Providing_Job()
        {
            const string JOB_ID = "id";
            var queuedJob = new Job
            {
                Id = JOB_ID,
                JobJson = JsonConvert.SerializeObject(new StoppableCrawlJob())
            };
            
            var logger = new LoggerConfiguration().CreateLogger();
            var mediator = new Mock<IMediator>();
            var nowProvider = new NowProvider();
            
            await using var context = Setup.CreateContext();

            context.Jobs.Add(queuedJob);

            context.SaveChanges();
            
            var service = new CrawlRunnerService(
                logger,
                mediator.Object,
                context,
                nowProvider
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);
            
            await service.StopAsync(cancellationTokenSource.Token);

            mediator
                .Verify(
                    mock => mock
                        .Send(
                            It.Is<RunCrawlRequest>(value => value.JobId == queuedJob.Id && value.JobActual != null),
                            It.IsAny<CancellationToken>()),
                    Times.Once());
        }
    }
}

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
            var logger = new LoggerConfiguration().CreateLogger();
            var mediator = new Mock<IMediator>();
            
            await using var context = Setup.CreateContext();

            var service = new CrawlerManagerService(
                logger,
                mediator.Object,
                context
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await service.StopAsync(cancellationTokenSource.Token);

            Assert.AreEqual(0, context.QueuedJobs.Count());
        }

        [TestMethod]
        public async Task Uses_Mediator_Providing_Job()
        {
            const string JOB_ID = "id";
            var queuedJob = new QueuedJob
            {
                Id = JOB_ID,
                JobJson = JsonConvert.SerializeObject(new StoppableCrawlJob())
            };
            
            var logger = new LoggerConfiguration().CreateLogger();
            var mediator = new Mock<IMediator>();
            
            await using var context = Setup.CreateContext();

            context.QueuedJobs.Add(queuedJob);

            context.SaveChanges();
            
            var service = new CrawlerManagerService(
                logger,
                mediator.Object,
                context
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);
            
            await service.StopAsync(cancellationTokenSource.Token);

            mediator
                .Verify(
                    mock => mock
                        .Send(
                            It.Is<RunCrawlRequest>(value => value.Job.Id == queuedJob.Id && value.JobData != null),
                            It.IsAny<CancellationToken>()),
                    Times.Once());
        }
    }
}

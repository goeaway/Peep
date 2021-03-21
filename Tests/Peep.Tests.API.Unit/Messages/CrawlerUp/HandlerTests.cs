using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Peep.API.Application.Requests.Messages.CrawlerUp;
using Peep.API.Models.Entities;
using Peep.API.Models.Enums;
using Peep.Core;
using Peep.Core.API.Providers;
using Peep.Core.Infrastructure.Messages;

namespace Peep.Tests.API.Unit.Messages.CrawlerUp
{
    [TestClass]
    [TestCategory("API - Unit - Crawler Up Handler")]
    public class HandlerTests
    {
        [TestMethod]
        public async Task Adds_JobCrawler_With_Id_Provided()
        {
            const string CRAWLER_ID = "id";

            var request = new CrawlerUpRequest
            {
                CrawlerId = CRAWLER_ID
            };

            await using var context = Setup.CreateContext();
            var now = new DateTime(2020, 01, 01);
            var nowProvider = new NowProvider(now);
            var mockSendEndpointProvider = new Mock<ISendEndpointProvider>();
            
            var handler = new CrawlerUpHandler(context, nowProvider, mockSendEndpointProvider.Object);

            await handler.Handle(request, CancellationToken.None);

            var jobCrawler = context.JobCrawlers.First();
            Assert.AreEqual(CRAWLER_ID, jobCrawler.CrawlerId);
            Assert.IsNull(jobCrawler.Job);
            Assert.AreEqual(now, jobCrawler.LastHeartbeat);
        }
        
        [TestMethod]
        public async Task Returns_Unit_Value_When_Successful()
        {
            const string CRAWLER_ID = "id";

            var request = new CrawlerUpRequest
            {
                CrawlerId = CRAWLER_ID
            };

            await using var context = Setup.CreateContext();
            var nowProvider = new NowProvider();
            var mockSendEndpointProvider = new Mock<ISendEndpointProvider>();
            
            var handler = new CrawlerUpHandler(context, nowProvider, mockSendEndpointProvider.Object);

            var response = (await handler.Handle(request, CancellationToken.None)).SuccessOrDefault;
            
            Assert.AreEqual(MediatR.Unit.Value, response);
        }

        [TestMethod]
        public async Task Returns_ErrorResponse_If_JobCrawler_Already_Exists_With_Id()
        {
            const string CRAWLER_ID = "id";

            var request = new CrawlerUpRequest()
            {
                CrawlerId = CRAWLER_ID
            };

            await using var context = Setup.CreateContext();

            await context.JobCrawlers.AddAsync(new JobCrawler
            {
                CrawlerId = CRAWLER_ID
            });

            await context.SaveChangesAsync();
            
            var nowProvider = new NowProvider();
            var mockSendEndpointProvider = new Mock<ISendEndpointProvider>();
            
            var handler = new CrawlerUpHandler(context, nowProvider, mockSendEndpointProvider.Object);

            var response = (await handler.Handle(request, CancellationToken.None)).ErrorOrDefault;
            
            Assert.AreEqual($"Crawler with id {CRAWLER_ID} already exists", response.Message);
        }

        [TestMethod]
        public async Task Sends_CrawlQueued_Message_If_RunningJob_Exists()
        {
            const string CRAWLER_ID = "crawler";
            const string JOB_ID = "job";
            
            var request = new CrawlerUpRequest()
            {
                CrawlerId = CRAWLER_ID
            };

            await using var context = Setup.CreateContext();

            await context.Jobs.AddAsync(new Job
            {
                Id = JOB_ID,
                State = JobState.Running,
                JobJson = JsonConvert.SerializeObject(new CrawlJob())
            });

            await context.SaveChangesAsync();

            var mockSendEndpoint = new Mock<ISendEndpoint>();
            var mockSendEndpointProvider = new Mock<ISendEndpointProvider>();
            mockSendEndpointProvider
                .Setup(
                    mock => mock.GetSendEndpoint(It.IsAny<Uri>()))
                .ReturnsAsync(mockSendEndpoint.Object);
            
            var nowProvider = new NowProvider();
            
            var handler = new CrawlerUpHandler(context, nowProvider, mockSendEndpointProvider.Object);

            await handler.Handle(request, CancellationToken.None);
            
            mockSendEndpointProvider
                .Verify(
                    mock => mock
                        .GetSendEndpoint(
                            It.Is<Uri>(
                                value => value.OriginalString == $"queue:crawl-queued-{CRAWLER_ID}")),
                    Times.Once());
            
            mockSendEndpoint
                .Verify(
                    mock => mock
                        .Send(
                            It.Is<CrawlQueued>(value => value.Job.Id == JOB_ID),
                            It.IsAny<CancellationToken>()),
                    Times.Once());
        }
        
        [TestMethod]
        public async Task Does_Not_Send_CrawlQueued_Message_If_No_RunningJob_Exists()
        {
            const string CRAWLER_ID = "crawler";
            
            var request = new CrawlerUpRequest()
            {
                CrawlerId = CRAWLER_ID
            };

            await using var context = Setup.CreateContext();

            var mockSendEndpointProvider = new Mock<ISendEndpointProvider>();
            
            var nowProvider = new NowProvider();
            
            var handler = new CrawlerUpHandler(context, nowProvider, mockSendEndpointProvider.Object);

            await handler.Handle(request, CancellationToken.None);
            
            mockSendEndpointProvider
                .Verify(
                    mock => mock
                        .GetSendEndpoint(It.IsAny<Uri>()),
                    Times.Never());
        }
    }
}
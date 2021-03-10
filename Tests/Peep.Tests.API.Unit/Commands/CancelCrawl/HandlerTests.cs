using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.API.Application.Requests.Commands.CancelCrawl;
using Peep.API.Models.Entities;
using Peep.Core.API.Providers;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Peep.API.Models.Enums;

namespace Peep.Tests.API.Unit.Commands.CancelCrawl
{
    [TestClass]
    [TestCategory("API - Unit - Cancel Crawl Handler")]
    public class HandlerTests
    {
        [TestMethod]
        [DataRow(JobState.Cancelled)]
        [DataRow(JobState.Complete)]
        [DataRow(JobState.Errored)]
        public async Task Returns_Error_If_Job_In_Uncancellable_State(JobState state)
        {
            const string CRAWL_ID = "crawl-id";
            var request = new CancelCrawlRequest(CRAWL_ID);

            await using var context = Setup.CreateContext();

            await context.Jobs.AddAsync(new Job
            {
                Id = CRAWL_ID, 
                State = state
            });
            
            var mockTokenProvider = new Mock<ICrawlCancellationTokenProvider>();

            var handler = new CancelCrawlHandler(context, mockTokenProvider.Object);

            var result = await handler.Handle(request, CancellationToken.None);

            var error = result.ErrorOrDefault;
            
            Assert.AreEqual(HttpStatusCode.BadRequest, error.StatusCode);
            Assert.AreEqual("Job not in a cancellable state", error.Message);
        }
        
        [TestMethod]
        public async Task Returns_Error_If_Crawl_Not_Found()
        {
            const string CRAWL_ID = "crawl-id";
            var request = new CancelCrawlRequest(CRAWL_ID);

            await using var context = Setup.CreateContext();

            var mockTokenProvider = new Mock<ICrawlCancellationTokenProvider>();

            var handler = new CancelCrawlHandler(context, mockTokenProvider.Object);

            var result = await handler.Handle(request, CancellationToken.None);

            var error = result.ErrorOrDefault;
            
            Assert.AreEqual(HttpStatusCode.NotFound, error.StatusCode);
            Assert.AreEqual("Job not found", error.Message);
        }

        [TestMethod]
        public async Task Sets_Queued_Job_To_Cancelled()
        {
            const string CRAWL_ID = "crawl-id";
            var request = new CancelCrawlRequest(CRAWL_ID);

            await using var context = Setup.CreateContext();

            await context.Jobs.AddAsync(new Job
            {
                Id = CRAWL_ID,
                State = JobState.Queued
            });

            await context.SaveChangesAsync();

            var mockTokenProvider = new Mock<ICrawlCancellationTokenProvider>();

            var handler = new CancelCrawlHandler(context, mockTokenProvider.Object);

            await handler.Handle(request, CancellationToken.None);

            var job = context.Jobs.First();
            Assert.AreEqual(JobState.Cancelled, job.State);
        }
        
        [TestMethod]
        public async Task Sets_Running_Job_To_Cancelled()
        {
            const string CRAWL_ID = "crawl-id";
            var request = new CancelCrawlRequest(CRAWL_ID);

            await using var context = Setup.CreateContext();

            await context.Jobs.AddAsync(new Job
            {
                Id = CRAWL_ID,
                State = JobState.Running
            });

            await context.SaveChangesAsync();

            var mockTokenProvider = new Mock<ICrawlCancellationTokenProvider>();

            var handler = new CancelCrawlHandler(context, mockTokenProvider.Object);

            await handler.Handle(request, CancellationToken.None);

            var job = context.Jobs.First();
            Assert.AreEqual(JobState.Cancelled, job.State);
        }

        [TestMethod]
        public async Task Cancels_Running_Job_With_CancellationTokenProvider()
        {
            const string CRAWL_ID = "crawl-id";
            var request = new CancelCrawlRequest(CRAWL_ID);

            await using var context = Setup.CreateContext();

            await context.Jobs.AddAsync(new Job
            {
                Id = CRAWL_ID,
                State = JobState.Running
            });

            var mockTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            mockTokenProvider
                .Setup(mock => mock.CancelJob(CRAWL_ID)).Returns(true);

            var handler = new CancelCrawlHandler(context, mockTokenProvider.Object);

            await handler.Handle(request, CancellationToken.None);

            mockTokenProvider.Verify(mock => mock.CancelJob(CRAWL_ID), Times.Once());
        }
    }
}

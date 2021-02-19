using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.API.Application.Requests.Commands.CancelCrawl;
using Peep.API.Models.Entities;
using Peep.Core.API.Exceptions;
using Peep.Core.API.Providers;
using Peep.Tests.API.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Peep.Tests.API.Unit.Commands.CancelCrawl
{
    [TestClass]
    [TestCategory("API - Unit - Cancel Crawl Handler")]
    public class HandlerTests
    {
        [TestMethod]
        public async Task Throws_If_Crawl_Not_Queued_Or_Running()
        {
            const string CRAWL_ID = "crawl-id";
            var request = new CancelCrawlRequest(CRAWL_ID);

            using var context = Setup.CreateContext();

            var mockTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            mockTokenProvider.Setup(mock => mock.CancelJob(CRAWL_ID)).Returns(false);

            var handler = new CancelCrawlHandler(context, mockTokenProvider.Object);

            await Assert.ThrowsExceptionAsync<RequestFailedException>(
                () => handler.Handle(request, CancellationToken.None));
        }

        [TestMethod]
        public async Task Dequeues_Crawl_If_Queued()
        {
            const string CRAWL_ID = "crawl-id";
            var request = new CancelCrawlRequest(CRAWL_ID);

            using var context = Setup.CreateContext();

            context.QueuedJobs.Add(new QueuedJob
            {
                Id = CRAWL_ID,
            });

            context.SaveChanges();

            var mockTokenProvider = new Mock<ICrawlCancellationTokenProvider>();

            var handler = new CancelCrawlHandler(context, mockTokenProvider.Object);

            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsFalse(context.QueuedJobs.Any());
        }

        [TestMethod]
        public async Task Cancels_Crawl_If_Running()
        {
            const string CRAWL_ID = "crawl-id";
            var request = new CancelCrawlRequest(CRAWL_ID);

            using var context = Setup.CreateContext();

            var mockTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            mockTokenProvider.Setup(mock => mock.CancelJob(CRAWL_ID)).Returns(true);

            var handler = new CancelCrawlHandler(context, mockTokenProvider.Object);

            var result = await handler.Handle(request, CancellationToken.None);

            mockTokenProvider.Verify(mock => mock.CancelJob(CRAWL_ID), Times.Once());
        }
    }
}

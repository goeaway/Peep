using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.API.Application.Commands.CancelQueuedCrawl;
using Peep.API.Application.Exceptions;
using Peep.API.Models.Entities;
using Peep.Tests.Core;
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

            var handler = new CancelCrawlHandler(context);

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

            var handler = new CancelCrawlHandler(context);

            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsFalse(context.QueuedJobs.Any());
        }

        [TestMethod]
        public async Task Cancels_Crawl_If_Running()
        {
            const string CRAWL_ID = "crawl-id";
            var request = new CancelCrawlRequest(CRAWL_ID);

            using var context = Setup.CreateContext();

            context.RunningJobs.Add(new RunningJob
            {
                Id = CRAWL_ID,
                Cancelled = false
            });

            context.SaveChanges();

            var handler = new CancelCrawlHandler(context);

            var result = await handler.Handle(request, CancellationToken.None);

            var foundRunning = context.RunningJobs.Find(CRAWL_ID);

            Assert.IsTrue(foundRunning.Cancelled);
        }
    }
}

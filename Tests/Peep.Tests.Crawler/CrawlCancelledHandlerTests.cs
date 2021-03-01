using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MassTransit.Testing;
using Moq;
using Peep.Core.API.Providers;
using Peep.Core.Infrastructure.Messages;
using Peep.Crawler;
using Peep.Crawler.Messages;
using Serilog;

namespace Peep.Tests.Crawler
{
    [TestClass]
    [TestCategory("Crawler - Cancel Crawl Handler")]
    public class CancelCrawlHandlerTests
    {
        [TestMethod]
        public async Task Tries_To_Remove_From_JobQueue()
        {
            const string JOB_ID = "id";
            
            var jobQueue = new Mock<IJobQueue>();
            jobQueue.Setup(mock => mock.TryRemove(JOB_ID))
                .Returns(true);
            
            var cancelProvider = new Mock<ICrawlCancellationTokenProvider>();
            
            var harness = new InMemoryTestHarness();
            var consumerHarness = harness
                .Consumer<CrawlCancelledConsumer>(() => new CrawlCancelledConsumer(
                    jobQueue.Object, 
                    cancelProvider.Object));

            await harness.Start();
            try
            {
                await harness.InputQueueSendEndpoint.Send<CrawlCancelled>(new CrawlCancelled
                {
                    CrawlId = JOB_ID
                });

                await consumerHarness.Consumed.Any<CrawlCancelled>();
                
                jobQueue.Verify(mock => mock.TryRemove(JOB_ID), Times.Once());
                
                cancelProvider.Verify(
                    mock => mock.CancelJob(It.IsAny<string>()), 
                    Times.Never());
            }
            finally
            {
                await harness.Stop();
            }
        }

        [TestMethod]
        public async Task Tries_To_Cancel_Running_Job_If_Not_In_Queue()
        {
            const string JOB_ID = "id";
            
            var jobQueue = new Mock<IJobQueue>();
            jobQueue.Setup(mock => mock.TryRemove(JOB_ID))
                .Returns(false);
            
            var cancelProvider = new Mock<ICrawlCancellationTokenProvider>();
            
            var harness = new InMemoryTestHarness();
            var consumerHarness = harness
                .Consumer<CrawlCancelledConsumer>(() => new CrawlCancelledConsumer(
                    jobQueue.Object, 
                    cancelProvider.Object));

            await harness.Start();
            try
            {
                await harness.InputQueueSendEndpoint.Send<CrawlCancelled>(new CrawlCancelled
                {
                    CrawlId = JOB_ID
                });

                await consumerHarness.Consumed.Any<CrawlCancelled>();
                
                cancelProvider.Verify(
                    mock => mock.CancelJob(JOB_ID), 
                    Times.Once());
            }
            finally
            {
                await harness.Stop();
            }
        }
    }
}

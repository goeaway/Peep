using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MassTransit.Testing;
using Moq;
using Peep.Core.Infrastructure.Messages;
using Peep.Crawler;
using Peep.Crawler.Messages;

namespace Peep.Tests.Crawler
{
    [TestClass]
    [TestCategory("Crawler - Queue Crawl Handler")]
    public class QueueCrawlHandlerTests
    {
        [TestMethod]
        public async Task Enqueues_Job_To_JobQueue()
        {
            const string JOB_ID = "id";
            var job = new IdentifiableCrawlJob
            {
                Id = JOB_ID
            };
            
            var harness = new InMemoryTestHarness();
            var jobQueue = new Mock<IJobQueue>();
            var consumerHarness = harness.Consumer<CrawlQueuedConsumer>(() => new CrawlQueuedConsumer(jobQueue.Object));

            await harness.Start();
            try
            {
                await harness.InputQueueSendEndpoint.Send<CrawlQueued>(
                    new CrawlQueued
                    {
                        Job = job
                    }    
                );

                await consumerHarness.Consumed.Any<CrawlQueued>();
                
                jobQueue.Verify(
                    mock => mock.Enqueue(
                        It.Is<IdentifiableCrawlJob>(j => j.Id == JOB_ID)), 
                    Times.Once());
            }
            finally
            {
                await harness.Stop();
            }
        }
    }
}

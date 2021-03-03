using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MassTransit.Testing;
using MediatR;
using Moq;
using Peep.Core.Infrastructure.Messages;
using Peep.Crawler;
using Peep.Crawler.Application.Requests.Commands.QueueCrawl;
using Peep.Crawler.Application.Services;
using Peep.Crawler.Messages;

namespace Peep.Tests.Crawler
{
    [TestClass]
    [TestCategory("Crawler - Queue Crawl Handler")]
    public class QueueCrawlHandlerTests
    {
        [TestMethod]
        public async Task Uses_Mediator_Passing_QueueCrawlRequest_With_Job()
        {
            const string JOB_ID = "id";
            var job = new IdentifiableCrawlJob
            {
                Id = JOB_ID
            };
            
            var harness = new InMemoryTestHarness();
            var mediator = new Mock<IMediator>();
            var consumerHarness = harness.Consumer(() => new CrawlQueuedConsumer(mediator.Object));

            await harness.Start();
            try
            {
                await harness.InputQueueSendEndpoint.Send(
                    new CrawlQueued
                    {
                        Job = job
                    }    
                );

                await consumerHarness.Consumed.Any<CrawlQueued>();
                
                mediator
                    .Verify(
                        mock => mock
                            .Send(
                                It.Is<QueueCrawlRequest>(value => value.Job.Id == job.Id),
                                It.IsAny<CancellationToken>()),
                        Times.Once());
            }
            finally
            {
                await harness.Stop();
            }
        }
    }
}

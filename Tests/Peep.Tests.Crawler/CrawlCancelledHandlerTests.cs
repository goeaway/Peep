using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MassTransit.Testing;
using MediatR;
using Moq;
using Peep.Core.API.Providers;
using Peep.Core.Infrastructure.Messages;
using Peep.Crawler;
using Peep.Crawler.Application.Requests.Commands.CancelCrawl;
using Peep.Crawler.Application.Services;
using Peep.Crawler.Messages;
using Serilog;

namespace Peep.Tests.Crawler
{
    [TestClass]
    [TestCategory("Crawler - Cancel Crawl Handler")]
    public class CancelCrawlHandlerTests
    {
        [TestMethod]
        public async Task Uses_Mediator_With_CancelCrawlRequest_And_ID()
        {
            const string JOB_ID = "id";

            var mediator = new Mock<IMediator>();
            
            var harness = new InMemoryTestHarness();
            var consumerHarness = harness
                .Consumer(() => new CrawlCancelledConsumer(mediator.Object));

            await harness.Start();
            try
            {
                await harness.InputQueueSendEndpoint.Send(new CrawlCancelled
                {
                    CrawlId = JOB_ID
                });

                await consumerHarness.Consumed.Any<CrawlCancelled>();
                
                mediator
                    .Verify(
                        mock => mock
                            .Send(It.Is<CancelCrawlRequest>(
                                value => value.CrawlId == JOB_ID),
                                It.IsAny<CancellationToken>())
                        , Times.Once());
            }
            finally
            {
                await harness.Stop();
            }
        }
    }
}

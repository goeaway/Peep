using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit.Testing;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.API.Application.Requests.Messages.CrawlerHeartbeat;
using Peep.API.Application.Requests.Messages.PushCrawlData;
using Peep.API.Messages;
using Peep.Core.Infrastructure.Messages;

namespace Peep.Tests.API.Unit.Consumers
{
    [TestClass]
    [TestCategory("API - Unit - Crawler Heartbeat Consumer")]
    public class CrawlerHeartbeatConsumerTests
    {
        [TestMethod]
        public async Task Uses_Mediator_To_Send_CrawlerHeartbeatRequest()
        {
            const string CRAWLER_ID = "crawler";
            var mediator = new Mock<IMediator>();
            
            var harness = new InMemoryTestHarness();
            var consumerHarness = harness
                .Consumer(() => new CrawlerHeartbeatConsumer(mediator.Object));

            await harness.Start();
            try
            {
                await harness.InputQueueSendEndpoint.Send(new CrawlerHeartbeat()
                {
                    CrawlerId = CRAWLER_ID
                });

                await consumerHarness.Consumed.Any<CrawlerHeartbeat>();
                
                mediator
                    .Verify(
                        mock => mock
                            .Send(It.Is<CrawlerHeartbeatRequest>(
                                    value => 
                                        value.CrawlerId == CRAWLER_ID),
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
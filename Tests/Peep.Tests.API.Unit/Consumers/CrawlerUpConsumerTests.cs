using System.Threading;
using System.Threading.Tasks;
using MassTransit.Testing;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.API.Application.Requests.Messages.CrawlerUp;
using Peep.API.Messages;
using Peep.Core.Infrastructure;
using Peep.Core.Infrastructure.Messages;

namespace Peep.Tests.API.Unit.Consumers
{
    [TestClass]
    [TestCategory("API - Unit - Crawler Up Consumer")]
    public class CrawlerUpConsumerTests
    {
        [TestMethod]
        public async Task Uses_Mediator_To_Create_CrawlerUpRequest()
        {
            var crawlerId = CrawlerId.FromMachineName();
            
            var mediator = new Mock<IMediator>();
            
            var harness = new InMemoryTestHarness();
            var consumerHarness = harness
                .Consumer(() => new CrawlerUpConsumer(mediator.Object, Setup.CreateEmptyLogger()));

            await harness.Start();
            try
            {
                await harness.InputQueueSendEndpoint.Send(new CrawlerUp()
                {
                    CrawlerId = crawlerId 
                });

                await consumerHarness.Consumed.Any<CrawlerUp>();
                
                mediator
                    .Verify(
                        mock => mock
                            .Send(It.Is<CrawlerUpRequest>(
                                    value => value.CrawlerId == crawlerId),
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
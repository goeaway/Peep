using System.Threading;
using System.Threading.Tasks;
using MassTransit.Testing;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.API.Application.Requests.Messages.CrawlerLeft;
using Peep.API.Messages;
using Peep.Core.Infrastructure;
using Peep.Core.Infrastructure.Messages;

namespace Peep.Tests.API.Unit.Consumers
{
    [TestClass]
    [TestCategory("API - Unit - Crawler Left Consumer")]
    public class CrawlerLeftConsumerTests
    {
        [TestMethod]
        public async Task Uses_Mediator_With_CrawlerId_And_JobId()
        {
            const string CRAWLER_ID = "crawler";
            const string JOB_ID = "id";
            
            var mediator = new Mock<IMediator>();
            
            var harness = new InMemoryTestHarness();
            var consumerHarness = harness
                .Consumer(() => new CrawlerLeftConsumer(mediator.Object, Setup.CreateEmptyLogger()));

            await harness.Start();
            try
            {
                await harness.InputQueueSendEndpoint.Send(new CrawlerLeft()
                {
                    CrawlerId = new CrawlerId(CRAWLER_ID),
                    JobId = JOB_ID,
                });

                await consumerHarness.Consumed.Any<CrawlerLeft>();
                
                mediator
                    .Verify(
                        mock => mock
                            .Send(It.Is<CrawlerLeftRequest>(
                                    value => 
                                        value.JobId == JOB_ID && value.CrawlerId.Value == CRAWLER_ID),
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
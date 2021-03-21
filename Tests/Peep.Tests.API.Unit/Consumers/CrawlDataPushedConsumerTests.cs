using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit.Testing;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.API.Application.Requests.Messages.PushCrawlData;
using Peep.API.Messages;
using Peep.Core;
using Peep.Core.Infrastructure.Messages;

namespace Peep.Tests.API.Unit.Consumers
{
    [TestClass]
    [TestCategory("API - Unit - Crawl Data Pushed Consumer")]
    public class CrawlDataPushedConsumerTests
    {
        [TestMethod]
        public async Task Uses_Mediator_With_JobId_And_Data()
        {
            const string JOB_ID = "id";
            const string DATA_URI = "http://localhost/";
            const string DATA_DATA = "data";
            var data = new ExtractedData
            {
                {new Uri(DATA_URI), new List<string> { DATA_DATA }}
            };
            
            var mediator = new Mock<IMediator>();
            
            var harness = new InMemoryTestHarness();
            var consumerHarness = harness
                .Consumer(() => new CrawlDataPushedConsumer(mediator.Object));

            await harness.Start();
            try
            {
                await harness.InputQueueSendEndpoint.Send(new CrawlDataPushed()
                {
                    JobId = JOB_ID,
                    Data = data
                });

                await consumerHarness.Consumed.Any<CrawlDataPushed>();
                
                mediator
                    .Verify(
                        mock => mock
                            .Send(It.Is<PushCrawlDataRequest>(
                                    value => 
                                        value.JobId == JOB_ID && value.Data.First().Key.AbsoluteUri == DATA_URI && value.Data.First().Value.First() == DATA_DATA),
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
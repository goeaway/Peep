using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit.Testing;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.API.Application.Requests.Commands.CancelCrawl;
using Peep.API.Application.Requests.Commands.PushCrawlData;
using Peep.API.Application.Requests.Commands.PushCrawlError;
using Peep.API.Messages;
using Peep.Core.Infrastructure.Messages;
using Peep.Data;

namespace Peep.Tests.API.Unit.Messages
{
    [TestClass]
    [TestCategory("API - Unit - Crawl Error Pushed Consumer")]
    public class CrawlErrorPushedConsumerTests
    {
        [TestMethod]
        public async Task Uses_Mediator_With_JobId_Message_StackTrace_And_Source()
        {
            const string JOB_ID = "id";
            const string MESSAGE = "message";
            const string SOURCE = "source";
            const string STACKTRACE = "stack";

            var mediator = new Mock<IMediator>();
            
            var harness = new InMemoryTestHarness();
            var consumerHarness = harness
                .Consumer(() => new CrawlErrorPushedConsumer(mediator.Object));

            await harness.Start();
            try
            {
                await harness.InputQueueSendEndpoint.Send(new CrawlErrorPushed()
                {
                    JobId = JOB_ID,
                    Source = SOURCE,
                    StackTrace = STACKTRACE,
                    Message = MESSAGE
                });

                await consumerHarness.Consumed.Any<CrawlErrorPushed>();
                
                mediator
                    .Verify(
                        mock => mock
                            .Send(It.Is<PushCrawlErrorRequest>(
                                    value => 
                                        value.JobId == JOB_ID &&
                                        value.Message == MESSAGE &&
                                        value.StackTrace == STACKTRACE && 
                                        value.Source == SOURCE),
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
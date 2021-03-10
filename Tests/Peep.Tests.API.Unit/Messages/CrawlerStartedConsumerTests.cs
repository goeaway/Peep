﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit.Testing;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.API.Application.Requests.Commands.CancelCrawl;
using Peep.API.Application.Requests.Commands.CrawlerStarted;
using Peep.API.Application.Requests.Commands.PushCrawlData;
using Peep.API.Messages;
using Peep.Core.Infrastructure.Messages;
using Peep.Data;

namespace Peep.Tests.API.Unit.Messages
{
    [TestClass]
    [TestCategory("API - Unit - Crawler Started Consumer")]
    public class CrawlerStartedConsumerTests
    {
        [TestMethod]
        public async Task Uses_Mediator_With_CrawlerId_And_JobId()
        {
            const string CRAWLER_ID = "crawler";
            const string JOB_ID = "id";
            
            var mediator = new Mock<IMediator>();
            
            var harness = new InMemoryTestHarness();
            var consumerHarness = harness
                .Consumer(() => new CrawlerStartedConsumer(mediator.Object));

            await harness.Start();
            try
            {
                await harness.InputQueueSendEndpoint.Send(new CrawlerStarted()
                {
                    CrawlerId = CRAWLER_ID,
                    JobId = JOB_ID,
                });

                await consumerHarness.Consumed.Any<CrawlerStarted>();
                
                mediator
                    .Verify(
                        mock => mock
                            .Send(It.Is<CrawlerStartedRequest>(
                                    value => 
                                        value.JobId == JOB_ID && value.CrawlerId == CRAWLER_ID),
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
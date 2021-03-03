using MassTransit;
using Peep.Core.Infrastructure.Messages;
using Peep.Core.Infrastructure.Queuing;
using Peep.Crawler;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Peep.Crawler.Application.Requests.Commands.QueueCrawl;

namespace Peep.Crawler.Messages
{
    public class CrawlQueuedConsumer : IConsumer<CrawlQueued>
    {
        private readonly IMediator _mediator;

        public CrawlQueuedConsumer(IMediator mediator)
        {
            _mediator = mediator;
        }

        public Task Consume(ConsumeContext<CrawlQueued> context)
            => _mediator.Send(new QueueCrawlRequest { Job = context.Message.Job });
    }
}

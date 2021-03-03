using MassTransit;
using Paramore.Brighter;
using Peep.Core.API.Providers;
using Peep.Core.Infrastructure.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Peep.Crawler.Application.Requests.Commands.CancelCrawl;
using Serilog;

namespace Peep.Crawler.Messages
{
    public class CrawlCancelledConsumer : IConsumer<CrawlCancelled>
    {
        private readonly IMediator _mediator;

        public CrawlCancelledConsumer(IMediator mediator)
        {
            _mediator = mediator;
        }

        public Task Consume(ConsumeContext<CrawlCancelled> context) 
            => _mediator.Send(new CancelCrawlRequest { CrawlId = context.Message.CrawlId });
    }
}

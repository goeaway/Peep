using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Peep.API.Application.Requests.Messages.CrawlerHeartbeat;
using Peep.Core.Infrastructure.Messages;

namespace Peep.API.Messages
{
    public class CrawlerHeartbeatConsumer : IConsumer<CrawlerHeartbeat>
    {
        private readonly IMediator _mediator;

        public CrawlerHeartbeatConsumer(IMediator mediator)
        {
            _mediator = mediator;
        }

        public Task Consume(ConsumeContext<CrawlerHeartbeat> context)
        {
            return _mediator.Send(new CrawlerHeartbeatRequest {CrawlerId = context.Message.CrawlerId});
        }
    }
}
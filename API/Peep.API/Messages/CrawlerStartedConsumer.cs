using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Peep.API.Application.Requests.Commands.CrawlerStarted;
using Peep.Core.Infrastructure.Messages;

namespace Peep.API.Messages
{
    public class CrawlerStartedConsumer : IConsumer<CrawlerStarted>
    {
        private readonly IMediator _mediator;

        public CrawlerStartedConsumer(IMediator mediator)
        {
            _mediator = mediator;
        }

        public Task Consume(ConsumeContext<CrawlerStarted> context)
        {
            return _mediator.Send(new CrawlerStartedRequest
            {
                CrawlerId = context.Message.CrawlerId,
                JobId = context.Message.JobId
            });
        }
    }
}
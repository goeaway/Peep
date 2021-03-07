using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Peep.API.Application.Requests.Commands.CrawlerFinished;
using Peep.Core.Infrastructure.Messages;

namespace Peep.API.Application.Messages
{
    public class CrawlerFinishedConsumer : IConsumer<CrawlerFinished>
    {
        private readonly IMediator _mediator;

        public CrawlerFinishedConsumer(IMediator mediator)
        {
            _mediator = mediator;
        }
        
        public Task Consume(ConsumeContext<CrawlerFinished> context)
        {
            return _mediator.Send(new CrawlerFinishedRequest
            {
                CrawlerId = context.Message.CrawlerId, 
                JobId = context.Message.JobId
            });
        }
    }
}
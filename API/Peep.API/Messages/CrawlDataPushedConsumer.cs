using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Peep.API.Application.Requests.Commands.PushCrawlData;
using Peep.Core.Infrastructure.Messages;

namespace Peep.API.Messages
{
    public class CrawlDataPushedConsumer : IConsumer<CrawlDataPushed>
    {
        private readonly IMediator _mediator;

        public CrawlDataPushedConsumer(IMediator mediator)
        {
            _mediator = mediator;
        }
        
        public Task Consume(ConsumeContext<CrawlDataPushed> context)
        {
            return _mediator.Send(new PushCrawlDataRequest
            {
                JobId = context.Message.JobId, 
                Data = context.Message.Data
            });
        }
    }
}
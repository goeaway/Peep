using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Peep.API.Application.Requests.Commands.PushCrawlError;
using Peep.Core.Infrastructure.Messages;

namespace Peep.API.Messages
{
    public class CrawlErrorPushedConsumer : IConsumer<CrawlErrorPushed>
    {
        private readonly IMediator _mediator;

        public CrawlErrorPushedConsumer(IMediator mediator)
        {
            _mediator = mediator;
        }

        public Task Consume(ConsumeContext<CrawlErrorPushed> context)
        {
            return _mediator.Send(new PushCrawlErrorRequest
            {
                JobId = context.Message.JobId,
                Message = context.Message.Message,
                StackTrace = context.Message.StackTrace,
                Source = context.Message.Source
            });
        }
    }
}
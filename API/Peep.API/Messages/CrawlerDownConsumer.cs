using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Peep.API.Application.Requests.Messages.CrawlerDown;
using Peep.Core.Infrastructure.Messages;
using Serilog;

namespace Peep.API.Messages
{
    public class CrawlerDownConsumer : IConsumer<CrawlerDown>
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        
        public CrawlerDownConsumer(IMediator mediator, ILogger logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public Task Consume(ConsumeContext<CrawlerDown> context)
        {
            _logger.Information($"Crawler {context.Message.CrawlerId} is offline");
            return _mediator.Send(new CrawlerDownRequest {CrawlerId = context.Message.CrawlerId});
        }
    }
}
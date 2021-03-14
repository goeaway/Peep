using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Peep.API.Application.Requests.Messages.CrawlerUp;
using Peep.Core.Infrastructure.Messages;
using Serilog;

namespace Peep.API.Messages
{
    public class CrawlerUpConsumer : IConsumer<CrawlerUp>
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public CrawlerUpConsumer(IMediator mediator, ILogger logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public Task Consume(ConsumeContext<CrawlerUp> context)
        {
            _logger.Information($"Crawler {context.Message.CrawlerId} is online");
            return _mediator.Send(new CrawlerUpRequest {CrawlerId = context.Message.CrawlerId});
        }
    }
}
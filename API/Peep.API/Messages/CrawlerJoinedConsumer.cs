using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Peep.API.Application.Requests.Messages.CrawlerJoined;
using Peep.Core.Infrastructure.Messages;
using Serilog;

namespace Peep.API.Messages
{
    public class CrawlerJoinedConsumer : IConsumer<CrawlerJoined>
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        
        public CrawlerJoinedConsumer(IMediator mediator, ILogger logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public Task Consume(ConsumeContext<CrawlerJoined> context)
        {
            _logger.Information($"Crawler {context.Message.CrawlerId} joining job {context.Message.JobId}");
            return _mediator.Send(new CrawlerJoinedRequest
            {
                CrawlerId = context.Message.CrawlerId,
                JobId = context.Message.JobId
            });
        }
    }
}
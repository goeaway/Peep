using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Peep.API.Application.Requests.Messages.CrawlerLeft;
using Peep.Core.Infrastructure.Messages;
using Serilog;

namespace Peep.API.Messages
{
    public class CrawlerLeftConsumer : IConsumer<CrawlerLeft>
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public CrawlerLeftConsumer(IMediator mediator, ILogger logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
        
        public Task Consume(ConsumeContext<CrawlerLeft> context)
        {
            _logger.Information($"Crawler {context.Message.CrawlerId} leaving job {context.Message.JobId}");
            return _mediator.Send(new CrawlerLeftRequest
            {
                CrawlerId = context.Message.CrawlerId, 
                JobId = context.Message.JobId
            });
        }
    }
}
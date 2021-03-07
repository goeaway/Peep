using MediatR;

namespace Peep.API.Application.Requests.Commands.CrawlerStarted
{
    public class CrawlerStartedRequest : IRequest<Unit>
    {
        public string CrawlerId { get; set; }
        public string JobId { get; set; }
    }
}
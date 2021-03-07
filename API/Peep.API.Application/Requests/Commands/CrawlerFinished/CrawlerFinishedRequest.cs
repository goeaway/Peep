using MediatR;

namespace Peep.API.Application.Requests.Commands.CrawlerFinished
{
    public class CrawlerFinishedRequest : IRequest<Unit>
    {
        public string CrawlerId { get; set; }
        public string JobId { get; set; }
    }
}
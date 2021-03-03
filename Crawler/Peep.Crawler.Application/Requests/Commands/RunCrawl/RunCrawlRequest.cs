using MediatR;

namespace Peep.Crawler.Application.Requests.Commands.RunCrawl
{
    public class RunCrawlRequest : IRequest<Unit>
    {
        public IdentifiableCrawlJob Job { get; set; }
    }
}
using MediatR;

namespace Peep.Crawler.Application.Requests.Commands.CancelCrawl
{
    public class CancelCrawlRequest : IRequest<Unit>
    {
        public string CrawlId { get; set; }
        
        public CancelCrawlRequest()
        {
        }
    }
}
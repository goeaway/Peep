using MediatR;

namespace Peep.Crawler.Application.Requests.Commands.QueueCrawl
{
    public class QueueCrawlRequest : IRequest<Unit>
    {
        public IdentifiableCrawlJob Job { get; set; }
        
        public QueueCrawlRequest()
        {
        }
    }
}
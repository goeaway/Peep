using MediatR;
using Peep.Core.API;

namespace Peep.Crawler.Application.Requests.Commands.QueueCrawl
{
    public class QueueCrawlRequest : IRequest<Either<Unit, HttpErrorResponse>>
    {
        public IdentifiableCrawlJob Job { get; set; }
        
        public QueueCrawlRequest()
        {
        }
    }
}
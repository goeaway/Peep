using MediatR;
using Peep.Core.API;

namespace Peep.Crawler.Application.Requests.Commands.CancelCrawl
{
    public class CancelCrawlRequest : IRequest<Either<Unit, HttpErrorResponse>>
    {
        public string CrawlId { get; set; }
        
        public CancelCrawlRequest()
        {
        }
    }
}
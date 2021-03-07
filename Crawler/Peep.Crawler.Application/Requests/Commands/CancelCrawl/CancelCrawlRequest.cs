using MediatR;
using Peep.Core.API;

namespace Peep.Crawler.Application.Requests.Commands.CancelCrawl
{
    public class CancelCrawlRequest : IRequest<Either<Unit, ErrorResponseDTO>>
    {
        public string CrawlId { get; set; }
        
        public CancelCrawlRequest()
        {
        }
    }
}
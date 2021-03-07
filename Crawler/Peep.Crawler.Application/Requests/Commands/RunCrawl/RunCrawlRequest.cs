using MediatR;
using Peep.Core.API;

namespace Peep.Crawler.Application.Requests.Commands.RunCrawl
{
    public class RunCrawlRequest : IRequest<Either<Unit, ErrorResponseDTO>>
    {
        public IdentifiableCrawlJob Job { get; set; }
    }
}
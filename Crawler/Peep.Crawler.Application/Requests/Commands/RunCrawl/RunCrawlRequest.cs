using MediatR;
using Peep.Core;
using Peep.Core.API;

namespace Peep.Crawler.Application.Requests.Commands.RunCrawl
{
    public class RunCrawlRequest : IRequest<Either<Unit, HttpErrorResponse>>
    {
        public IdentifiableCrawlJob Job { get; set; }
    }
}
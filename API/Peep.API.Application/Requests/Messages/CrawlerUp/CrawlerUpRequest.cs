using MediatR;
using Peep.Core.API;
using Peep.Core.Infrastructure;

namespace Peep.API.Application.Requests.Messages.CrawlerUp
{
    public class CrawlerUpRequest : IRequest<Either<Unit, MessageErrorResponse>>
    {
        public CrawlerId CrawlerId { get; set; }
    }
}
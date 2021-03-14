using MediatR;
using Peep.Core.API;
using Peep.Core.Infrastructure;

namespace Peep.API.Application.Requests.Messages.CrawlerDown
{
    public class CrawlerDownRequest : IRequest<Either<Unit, MessageErrorResponse>>
    {
        public CrawlerId CrawlerId { get; set; }
    }
}
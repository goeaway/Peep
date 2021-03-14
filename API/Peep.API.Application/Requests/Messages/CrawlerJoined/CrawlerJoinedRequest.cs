using MediatR;
using Peep.Core.API;
using Peep.Core.Infrastructure;

namespace Peep.API.Application.Requests.Messages.CrawlerJoined
{
    public class CrawlerJoinedRequest : IRequest<Either<Unit, MessageErrorResponse>>
    {
        public CrawlerId CrawlerId { get; set; }
        public string JobId { get; set; }
    }
}
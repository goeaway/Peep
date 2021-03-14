using MediatR;
using Peep.Core.API;
using Peep.Core.Infrastructure;

namespace Peep.API.Application.Requests.Messages.CrawlerLeft
{
    public class CrawlerLeftRequest : IRequest<Either<Unit, MessageErrorResponse>>
    {
        public CrawlerId CrawlerId { get; set; }
        public string JobId { get; set; }
    }
}
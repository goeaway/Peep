using MediatR;
using Peep.Core;
using Peep.Core.API;

namespace Peep.API.Application.Requests.Messages.PushCrawlData
{
    public class PushCrawlDataRequest : IRequest<Either<Unit, HttpErrorResponse>>
    {
        public string JobId { get; set; }
        public ExtractedData Data { get; set; }
    }
}
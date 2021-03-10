using MediatR;
using Peep.Core.API;
using Peep.Data;

namespace Peep.API.Application.Requests.Commands.PushCrawlData
{
    public class PushCrawlDataRequest : IRequest<Either<Unit, ErrorResponseDTO>>
    {
        public string JobId { get; set; }
        public ExtractedData Data { get; set; }
    }
}
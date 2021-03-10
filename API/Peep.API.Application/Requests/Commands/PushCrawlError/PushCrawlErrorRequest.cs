using MediatR;
using Peep.Core.API;
using Peep.Data;

namespace Peep.API.Application.Requests.Commands.PushCrawlError
{
    public class PushCrawlErrorRequest : IRequest<Either<Unit, ErrorResponseDTO>>
    {
        public string JobId { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string Source { get; set; }
    }
}
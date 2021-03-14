using MediatR;
using Peep.Core.API;

namespace Peep.API.Application.Requests.Messages.PushCrawlError
{
    public class PushCrawlErrorRequest : IRequest<Either<Unit, HttpErrorResponse>>
    {
        public string JobId { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string Source { get; set; }
    }
}
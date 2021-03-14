using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Peep.API.Models.Entities;
using Peep.API.Persistence;
using Peep.Core.API;

namespace Peep.API.Application.Requests.Messages.PushCrawlError
{
    public class PushCrawlErrorHandler : IRequestHandler<PushCrawlErrorRequest, Either<Unit, HttpErrorResponse>>
    {
        private readonly PeepApiContext _context;

        public PushCrawlErrorHandler(PeepApiContext context)
        {
            _context = context;
        }

        public async Task<Either<Unit, HttpErrorResponse>> Handle(PushCrawlErrorRequest request, CancellationToken cancellationToken)
        {
            var foundJob = await _context.Jobs.FindAsync(request.JobId);

            if (foundJob == null)
            {
                return new HttpErrorResponse
                {
                    Message = "Could not find job",
                    StatusCode = HttpStatusCode.NotFound
                };
            }

            foundJob.JobErrors ??= new List<JobError>();
            
            foundJob.JobErrors.Add(new JobError
            {
                Message = request.Message,
                Source = request.Source,
                StackTrace = request.StackTrace
            });

            await _context.SaveChangesAsync(cancellationToken);
            
            return Unit.Value;
        }
    }
}
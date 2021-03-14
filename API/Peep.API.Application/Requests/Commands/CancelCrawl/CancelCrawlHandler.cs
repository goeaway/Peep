using MediatR;
using Peep.API.Models.DTOs;
using Peep.API.Persistence;
using Peep.Core.API.Providers;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Peep.API.Models.Enums;
using Peep.Core.API;

namespace Peep.API.Application.Requests.Commands.CancelCrawl
{
    public class CancelCrawlHandler : IRequestHandler<CancelCrawlRequest, Either<CancelCrawlResponseDto, HttpErrorResponse>>
    {
        private readonly PeepApiContext _context;
        private readonly ICrawlCancellationTokenProvider _tokenProvider;

        public CancelCrawlHandler(PeepApiContext context,
            ICrawlCancellationTokenProvider tokenProvider)
        {
            _context = context;
            _tokenProvider = tokenProvider;
        }

        public async Task<Either<CancelCrawlResponseDto, HttpErrorResponse>> Handle(CancelCrawlRequest request, CancellationToken cancellationToken)
        {
            // try and find in the db, remove from there
            var foundJob = await _context.Jobs.FindAsync(request.CrawlId);

            if (foundJob == null)
            {
                return new HttpErrorResponse
                {
                    Message = "Job not found",
                    StatusCode = HttpStatusCode.NotFound
                };
            }

            switch (foundJob.State)
            {
                case JobState.Queued:
                    foundJob.State = JobState.Cancelled;
                    await _context.SaveChangesAsync(cancellationToken);
                    return new CancelCrawlResponseDto();
                case JobState.Running:
                    foundJob.State = JobState.Cancelled;
                    await _context.SaveChangesAsync(cancellationToken);
                    _tokenProvider.CancelJob(request.CrawlId);
                    return new CancelCrawlResponseDto();
                default:
                    return new HttpErrorResponse
                    {
                        Message = "Job not in a cancellable state",
                        StatusCode = HttpStatusCode.BadRequest
                    };
            }
        }
    }
}

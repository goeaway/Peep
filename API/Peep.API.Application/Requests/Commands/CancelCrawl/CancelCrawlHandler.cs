using MediatR;
using Peep.API.Models.DTOs;
using Peep.API.Persistence;
using Peep.Core.API.Providers;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Peep.Core.API;

namespace Peep.API.Application.Requests.Commands.CancelCrawl
{
    public class CancelCrawlHandler : IRequestHandler<CancelCrawlRequest, Either<CancelCrawlResponseDto, ErrorResponseDTO>>
    {
        private readonly PeepApiContext _context;
        private readonly ICrawlCancellationTokenProvider _tokenProvider;

        public CancelCrawlHandler(PeepApiContext context,
            ICrawlCancellationTokenProvider tokenProvider)
        {
            _context = context;
            _tokenProvider = tokenProvider;
        }

        public async Task<Either<CancelCrawlResponseDto, ErrorResponseDTO>> Handle(CancelCrawlRequest request, CancellationToken cancellationToken)
        {
            // try and find in the db, remove from there
            var foundQueued = await _context.QueuedJobs.FindAsync(request.CrawlId);

            if (foundQueued != null)
            {
                // dequeue then return
                _context.QueuedJobs.Remove(foundQueued);
                await _context.SaveChangesAsync(cancellationToken);

                return new CancelCrawlResponseDto();
            }

            // get the token provider to cancel the job
            // if true, the job was cancelled,
            // if not, then no job was running with this id
            if (_tokenProvider.CancelJob(request.CrawlId))
            {
                return new CancelCrawlResponseDto();
            }

            // if we got here the crawl job was not found
            return new ErrorResponseDTO
            {
                Message = "Crawl not found",
                StatusCode = HttpStatusCode.NotFound
            };
        }
    }
}

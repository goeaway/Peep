using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Peep.API.Models.Enums;
using Peep.API.Persistence;
using Peep.Core.API;
using Peep.Core.API.Providers;

namespace Peep.API.Application.Requests.Messages.CrawlerJoined
{
    public class CrawlerJoinedHandler : IRequestHandler<CrawlerJoinedRequest, Either<Unit, MessageErrorResponse>>
    {
        private readonly PeepApiContext _context;
        private readonly INowProvider _nowProvider;

        public CrawlerJoinedHandler(PeepApiContext context, INowProvider nowProvider)
        {
            _context = context;
            _nowProvider = nowProvider;
        }

        public async Task<Either<Unit, MessageErrorResponse>> Handle(CrawlerJoinedRequest request, CancellationToken cancellationToken)
        {
            var foundCrawler = await _context.JobCrawlers.FindAsync(request.CrawlerId);

            if (foundCrawler == null)
            {
                return new MessageErrorResponse
                {
                    Message = $"Crawler with id {request.CrawlerId} not found"
                };
            }
            
            var foundJob = await _context.Jobs.FindAsync(request.JobId);

            if (foundJob == null)
            {
                return new MessageErrorResponse
                {
                    Message = $"Job with id {request.JobId} not found"
                };
            }

            if (foundJob.State != JobState.Running)
            {
                return new MessageErrorResponse
                {
                    Message = $"Cannot run job in current state ({foundJob.State})"
                };
            }

            foundCrawler.Job = foundJob;
            foundCrawler.LastHeartbeat = _nowProvider.Now;

            await _context.SaveChangesAsync(cancellationToken);
            
            return Unit.Value;
        }
    }
}
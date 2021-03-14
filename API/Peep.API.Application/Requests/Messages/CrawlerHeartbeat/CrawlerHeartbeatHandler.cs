using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Peep.API.Persistence;
using Peep.Core.API;
using Peep.Core.API.Providers;

namespace Peep.API.Application.Requests.Messages.CrawlerHeartbeat
{
    public class CrawlerHeartbeatHandler : IRequestHandler<CrawlerHeartbeatRequest, Either<Unit, MessageErrorResponse>>
    {
        private readonly PeepApiContext _context;
        private readonly INowProvider _nowProvider;

        public CrawlerHeartbeatHandler(PeepApiContext context, INowProvider nowProvider)
        {
            _context = context;
            _nowProvider = nowProvider;
        }

        public async Task<Either<Unit, MessageErrorResponse>> Handle(CrawlerHeartbeatRequest request, CancellationToken cancellationToken)
        {
            var foundJobCrawler = await _context.JobCrawlers.FindAsync(request.CrawlerId);

            if (foundJobCrawler == null)
            {
                return new MessageErrorResponse
                {
                    Message = $"Crawler with id {request.CrawlerId} not found"
                };
            }
            
            foundJobCrawler.LastHeartbeat = _nowProvider.Now;
            
            await _context.SaveChangesAsync(cancellationToken);
            
            return Unit.Value;
        }
    }
}
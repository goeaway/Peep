using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Peep.API.Persistence;
using Peep.Core.API;
using Peep.Core.API.Providers;

namespace Peep.API.Application.Requests.Messages.CrawlerLeft
{
    public class CrawlerLeftHandler : IRequestHandler<CrawlerLeftRequest, Either<Unit, MessageErrorResponse>>
    {
        private readonly PeepApiContext _context;
        private readonly INowProvider _nowProvider;

        public CrawlerLeftHandler(PeepApiContext context, INowProvider nowProvider)
        {
            _context = context;
            _nowProvider = nowProvider;
        }

        public async Task<Either<Unit, MessageErrorResponse>> Handle(CrawlerLeftRequest request, CancellationToken cancellationToken)
        {
            var foundJobCrawler = await _context
                .JobCrawlers
                .FirstOrDefaultAsync(jc => jc.CrawlerId == request.CrawlerId, cancellationToken);

            if (foundJobCrawler == null)
            {
                return new MessageErrorResponse
                {
                    Message = $"Crawler with id {request.CrawlerId} not found"
                };
            }

            foundJobCrawler.JobId = null;
            foundJobCrawler.LastHeartbeat = _nowProvider.Now;

            await _context.SaveChangesAsync(cancellationToken);
            
            return Unit.Value;
        }
    }
}
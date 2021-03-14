using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Peep.API.Persistence;
using Peep.Core.API;

namespace Peep.API.Application.Requests.Messages.CrawlerDown
{
    public class CrawlerDownHandler : IRequestHandler<CrawlerDownRequest, Either<Unit, MessageErrorResponse>>
    {
        private readonly PeepApiContext _context;

        public CrawlerDownHandler(PeepApiContext context)
        {
            _context = context;
        }

        public async Task<Either<Unit, MessageErrorResponse>> Handle(CrawlerDownRequest request, CancellationToken cancellationToken)
        {
            var found = await _context.JobCrawlers.FindAsync(request.CrawlerId);
            if (found == null)
            {
                return new MessageErrorResponse
                {
                    Message = "Crawler not found"
                };
            }

            // TODO do anything else if jobcrawler has a job attached?
            
            _context.JobCrawlers.Remove(found);

            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
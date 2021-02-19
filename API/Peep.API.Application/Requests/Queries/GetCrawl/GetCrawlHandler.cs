using AutoMapper;
using MediatR;
using Peep.API.Models.DTOs;
using Peep.API.Persistence;
using Peep.Core.API.Exceptions;
using System.Threading;
using System.Threading.Tasks;

namespace Peep.API.Application.Requests.Queries.GetCrawl
{
    public class GetCrawlHandler : IRequestHandler<GetCrawlRequest, GetCrawlResponseDTO>
    {
        private readonly PeepApiContext _context;
        private readonly IMapper _mapper;

        public GetCrawlHandler(
            PeepApiContext context,
            IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<GetCrawlResponseDTO> Handle(GetCrawlRequest request, CancellationToken cancellationToken)
        {
            // check queued table
            var foundQueued = await _context.QueuedJobs.FindAsync(request.CrawlId);
            if (foundQueued != null)
            {
                return _mapper.Map<GetCrawlResponseDTO>(foundQueued);
            }

            // check for running
            var foundRunning = await _context.RunningJobs.FindAsync(request.CrawlId);
            if (foundRunning != null)
            {
                return _mapper.Map<GetCrawlResponseDTO>(foundRunning);
            }

            // check completed table
            var foundCompleted = await _context.CompletedJobs.FindAsync(request.CrawlId);
            if (foundCompleted != null)
            {
                return _mapper.Map<GetCrawlResponseDTO>(foundCompleted);
            }

            // throw that it's not found
            throw new RequestFailedException("Crawl job not found", System.Net.HttpStatusCode.NotFound);
        }
    }
}

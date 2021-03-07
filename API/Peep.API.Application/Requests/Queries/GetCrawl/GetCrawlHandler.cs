using System.Net;
using AutoMapper;
using MediatR;
using Peep.API.Models.DTOs;
using Peep.API.Persistence;
using Peep.Core.Infrastructure.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Peep.Core.API;
using Peep.Data;

namespace Peep.API.Application.Requests.Queries.GetCrawl
{
    public class GetCrawlHandler : IRequestHandler<GetCrawlRequest, Either<GetCrawlResponseDto, ErrorResponseDTO>>
    {
        private readonly PeepApiContext _context;
        private readonly IMapper _mapper;
        private readonly ICrawlDataSinkManager<ExtractedData> _dataManager;

        public GetCrawlHandler(
            PeepApiContext context,
            ICrawlDataSinkManager<ExtractedData> dataManager,
            IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
            _dataManager = dataManager;
        }

        public async Task<Either<GetCrawlResponseDto, ErrorResponseDTO>> Handle(GetCrawlRequest request, CancellationToken cancellationToken)
        {
            // check queued table
            var foundQueued = await _context.QueuedJobs.FindAsync(request.CrawlId);
            if (foundQueued != null)
            {
                return _mapper.Map<GetCrawlResponseDto>(foundQueued);
            }

            // check for running
            var foundRunning = await _context.RunningJobs.FindAsync(request.CrawlId);
            if (foundRunning != null)
            {
                var mapped = _mapper.Map<GetCrawlResponseDto>(foundRunning);
                // get data
                mapped.Data = await _dataManager.GetData(request.CrawlId);
                return mapped;
            }

            // check completed table
            var foundCompleted = await _context.CompletedJobs
                .Include(cj => cj.CompletedJobData)
                .FirstOrDefaultAsync(
                    cj => cj.Id == request.CrawlId,
                    cancellationToken);
            if (foundCompleted != null)
            {
                return _mapper.Map<GetCrawlResponseDto>(foundCompleted);
            }

            // return error that it was not found
            return new ErrorResponseDTO
            {
                StatusCode = HttpStatusCode.NotFound,
                Message = "Crawl not found"
            };
        }
    }
}

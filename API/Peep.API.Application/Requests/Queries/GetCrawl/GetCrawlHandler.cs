using System.Net;
using AutoMapper;
using MediatR;
using Peep.API.Models.DTOs;
using Peep.API.Persistence;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Peep.Core.API;

namespace Peep.API.Application.Requests.Queries.GetCrawl
{
    public class GetCrawlHandler : IRequestHandler<GetCrawlRequest, Either<GetCrawlResponseDto, ErrorResponseDTO>>
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

        public async Task<Either<GetCrawlResponseDto, ErrorResponseDTO>> Handle(GetCrawlRequest request, CancellationToken cancellationToken)
        {
            var foundCompleted = await _context.Jobs
                .Include(j => j.JobData)
                .Include(j => j.JobErrors)
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

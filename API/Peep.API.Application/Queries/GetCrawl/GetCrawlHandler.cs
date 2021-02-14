using AutoMapper;
using MediatR;
using Peep.API.Application.Providers;
using Peep.API.Models.DTOs;
using Peep.API.Persistence;
using Peep.Core.API.Exceptions;
using System.Threading;
using System.Threading.Tasks;

namespace Peep.API.Application.Queries.GetCrawl
{
    public class GetCrawlHandler : IRequestHandler<GetCrawlRequest, GetCrawlResponseDTO>
    {
        private readonly PeepApiContext _context;
        private readonly IMapper _mapper;
        private readonly IRunningCrawlJobProvider _runningCrawlJobRepository;

        public GetCrawlHandler(
            PeepApiContext context, 
            IMapper mapper,
            IRunningCrawlJobProvider runningCrawlJobRepository)
        {
            _context = context;
            _mapper = mapper;
            _runningCrawlJobRepository = runningCrawlJobRepository;
        }

        public async Task<GetCrawlResponseDTO> Handle(GetCrawlRequest request, CancellationToken cancellationToken)
        {
            // check queued table
            var foundQueued = await _context.QueuedJobs.FindAsync(request.CrawlId);
            if(foundQueued != null)
            {
                return _mapper.Map<GetCrawlResponseDTO>(foundQueued);
            }

            // check for running
            var foundRunning = await _runningCrawlJobRepository.GetRunningJob(request.CrawlId);
            if(foundRunning != null)
            {
                return _mapper.Map<GetCrawlResponseDTO>(foundRunning);
            }

            // check completed table
            var foundCompleted = await _context.CompletedJobs.FindAsync(request.CrawlId);
            if(foundCompleted != null)
            {
                return _mapper.Map<GetCrawlResponseDTO>(foundCompleted);
            }
            
            // check errored table
            var foundErrored = await _context.ErroredJobs.FindAsync(request.CrawlId);
            if(foundErrored != null)
            {
                return _mapper.Map<GetCrawlResponseDTO>(foundErrored);
            }

            // throw that it's not found
            throw new RequestFailedException("Crawl job not found", System.Net.HttpStatusCode.NotFound);
        }
    }
}

using MediatR;
using Peep.API.Application.Providers;
using Peep.API.Models.DTOs;
using Peep.API.Persistence;
using Peep.Core.API.Exceptions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Peep.API.Application.Commands.CancelQueuedCrawl
{
    public class CancelCrawlHandler : IRequestHandler<CancelCrawlRequest, CancelCrawlResponseDTO>
    {
        private readonly PeepApiContext _context;
        private readonly ICrawlCancellationTokenProvider _tokenProvider;

        public CancelCrawlHandler(PeepApiContext context,
            ICrawlCancellationTokenProvider tokenProvider)
        {
            _context = context;
            _tokenProvider = tokenProvider;
        }

        public async Task<CancelCrawlResponseDTO> Handle(CancelCrawlRequest request, CancellationToken cancellationToken)
        {
            // try and find in the db, remove from there
            var foundQueued = await _context.QueuedJobs.FindAsync(request.CrawlId);

            if(foundQueued != null)
            {
                // dequeue then return
                _context.QueuedJobs.Remove(foundQueued);
                await _context.SaveChangesAsync();

                return new CancelCrawlResponseDTO();
            }

            // get the token provider to cancel the job
            // if true, the job was cancelled,
            // if not, then no job was running with this id
            if(_tokenProvider.CancelJob(request.CrawlId))
            {
                return new CancelCrawlResponseDTO();
            }

            // if we got here the crawl job was not found
            throw new RequestFailedException("Crawl not found", HttpStatusCode.NotFound);
        }
    }
}

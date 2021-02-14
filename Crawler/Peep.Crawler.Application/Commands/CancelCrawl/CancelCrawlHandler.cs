using MediatR;
using Peep.Core.API.Exceptions;
using Peep.Crawler.Application.Providers;
using Peep.Crawler.Models.DTOs;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Peep.Crawler.Application.Commands.CancelQueuedCrawl
{
    public class CancelCrawlHandler : IRequestHandler<CancelCrawlRequest, CancelCrawlResponseDTO>
    {
        private readonly ICrawlCancellationTokenProvider _tokenProvider;

        public CancelCrawlHandler(ICrawlCancellationTokenProvider tokenProvider)
        {
            _tokenProvider = tokenProvider;
        }

        public Task<CancelCrawlResponseDTO> Handle(CancelCrawlRequest request, CancellationToken cancellationToken)
        {

            // get the token provider to cancel the job
            // if true, the job was cancelled,
            // if not, then no job was running with this id
            if(_tokenProvider.CancelJob(request.CrawlId))
            {
                return Task.FromResult(new CancelCrawlResponseDTO());
            }

            // if we got here the crawl job was not found
            throw new RequestFailedException("Crawl not found", HttpStatusCode.NotFound);
        }
    }
}

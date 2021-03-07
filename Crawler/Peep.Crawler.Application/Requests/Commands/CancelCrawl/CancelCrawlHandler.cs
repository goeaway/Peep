using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Peep.Core.API;
using Peep.Core.API.Providers;
using Peep.Crawler.Application.Services;

namespace Peep.Crawler.Application.Requests.Commands.CancelCrawl
{
    public class CancelCrawlHandler : IRequestHandler<CancelCrawlRequest, Either<Unit, ErrorResponseDTO>>
    {
        private readonly IJobQueue _jobQueue;
        private readonly ICrawlCancellationTokenProvider _crawlCancellationTokenProvider;

        public CancelCrawlHandler(
            IJobQueue jobQueue, 
            ICrawlCancellationTokenProvider crawlCancellationTokenProvider)
        {
            _jobQueue = jobQueue;
            _crawlCancellationTokenProvider = crawlCancellationTokenProvider;
        }

        public Task<Either<Unit, ErrorResponseDTO>> Handle(CancelCrawlRequest request, CancellationToken cancellationToken)
        {
            if (_jobQueue.TryRemove(request.CrawlId))
            {
                return Task.FromResult(new Either<Unit, ErrorResponseDTO>(Unit.Value));
            }

            _crawlCancellationTokenProvider.CancelJob(request.CrawlId);

            return Task.FromResult(new Either<Unit, ErrorResponseDTO>(Unit.Value));
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Peep.API.Application.Managers;

namespace Peep.API.Application.Requests.Commands.CrawlerFinished
{
    public class CrawlerFinishedHandler : IRequestHandler<CrawlerFinishedRequest, Unit>
    {
        private readonly ICrawlerManager _crawlerManager;

        public CrawlerFinishedHandler(ICrawlerManager crawlerManager)
        {
            _crawlerManager = crawlerManager;
        }

        public Task<Unit> Handle(CrawlerFinishedRequest request, CancellationToken cancellationToken)
        {
            _crawlerManager.Finish(request.CrawlerId, request.JobId);

            return Task.FromResult(Unit.Value);
        }
    }
}
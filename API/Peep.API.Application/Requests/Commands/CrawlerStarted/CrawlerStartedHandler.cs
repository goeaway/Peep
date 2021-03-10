using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Peep.API.Application.Managers;

namespace Peep.API.Application.Requests.Commands.CrawlerStarted
{
    public class CrawlerStartedHandler : IRequestHandler<CrawlerStartedRequest, Unit>
    {
        private readonly ICrawlerManager _crawlerManager;

        public CrawlerStartedHandler(ICrawlerManager crawlerManager)
        {
            _crawlerManager = crawlerManager;
        }

        public Task<Unit> Handle(CrawlerStartedRequest request, CancellationToken cancellationToken)
        {
            _crawlerManager.Start(request.CrawlerId, request.JobId);

            return Task.FromResult(Unit.Value);
        }
    }
}
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Peep.API.Application.Requests.Commands.CrawlerStarted
{
    public class CrawlerStartedHandler : IRequestHandler<CrawlerStartedRequest, Unit>
    {
        public Task<Unit> Handle(CrawlerStartedRequest request, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
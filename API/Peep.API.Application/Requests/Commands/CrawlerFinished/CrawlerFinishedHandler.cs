using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Peep.API.Application.Requests.Commands.CrawlerFinished
{
    public class CrawlerFinishedHandler : IRequestHandler<CrawlerFinishedRequest, Unit>
    {
        public Task<Unit> Handle(CrawlerFinishedRequest request, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
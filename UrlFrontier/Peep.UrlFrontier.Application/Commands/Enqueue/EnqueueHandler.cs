using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Peep.UrlFrontier.Application.Commands.Enqueue
{
    public class EnqueueHandler : IRequestHandler<EnqueueRequest, Unit>
    {
        public Task<Unit> Handle(EnqueueRequest request, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
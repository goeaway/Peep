using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Peep.UrlFrontier.Application.Commands.Monitor
{
    public class MonitorHandler : IRequestHandler<MonitorRequest, Unit>
    {
        public Task<Unit> Handle(MonitorRequest request, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
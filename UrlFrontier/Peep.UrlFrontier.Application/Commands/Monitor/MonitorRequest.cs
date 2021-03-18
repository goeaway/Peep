using MediatR;

namespace Peep.UrlFrontier.Application.Commands.Monitor
{
    public struct MonitorRequest : IRequest<Unit>
    {
    }
}
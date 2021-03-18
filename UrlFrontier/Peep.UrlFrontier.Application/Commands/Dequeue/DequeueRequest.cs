using System;
using MediatR;

namespace Peep.UrlFrontier.Application.Commands.Dequeue
{
    public class DequeueRequest : IRequest<Uri>
    {
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Peep.UrlFrontier.Application.Commands.Dequeue
{
    public class DequeueHandler : IRequestHandler<DequeueRequest, Uri>
    {
        public Task<Uri> Handle(DequeueRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
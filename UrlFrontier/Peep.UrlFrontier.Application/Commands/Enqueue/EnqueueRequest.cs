using System;
using System.Collections.Generic;
using MediatR;

namespace Peep.UrlFrontier.Application.Commands.Enqueue
{
    public class EnqueueRequest : IRequest<Unit>
    {
        public Uri Source { get; set; }
        public IEnumerable<Uri> Uris { get; set; }
    }
}
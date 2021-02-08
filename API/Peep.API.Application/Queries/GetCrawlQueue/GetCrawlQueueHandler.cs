using MediatR;
using Peep.API.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Peep.API.Application.Queries.GetCrawlQueue
{
    public class GetCrawlQueueHandler : IRequestHandler<GetCrawlQueueRequest, IEnumerable<QueuedCrawlDTO>>
    {
        public Task<IEnumerable<QueuedCrawlDTO>> Handle(GetCrawlQueueRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

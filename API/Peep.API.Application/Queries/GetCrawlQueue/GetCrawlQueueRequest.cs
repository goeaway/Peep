using MediatR;
using Peep.API.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.API.Application.Queries.GetCrawlQueue
{
    public class GetCrawlQueueRequest : IRequest<IEnumerable<QueuedCrawlDTO>>
    {
    }
}

using MediatR;
using Peep.API.Models.DTOs;
using Peep.Core;

namespace Peep.API.Application.Commands.QueueCrawl
{
    public class QueueCrawlRequest : IRequest<QueueCrawlResponseDTO>
    {
        public StoppableCrawlJob Job { get; set; }

        public QueueCrawlRequest(StoppableCrawlJob job)
        {
            Job = job;
        }
    }
}
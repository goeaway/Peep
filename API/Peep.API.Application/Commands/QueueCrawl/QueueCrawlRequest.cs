using MediatR;
using Peep.API.Models.DTOs;
using Peep.Core;

namespace Peep.API.Application.Commands.QueueCrawl
{
    public class QueueCrawlRequest : IRequest<QueueCrawlResponseDTO>
    {
        public CrawlJob Job { get; set; }

        public QueueCrawlRequest(CrawlJob job)
        {
            Job = job;
        }
    }
}
using MediatR;
using Peep.Crawler.Models.DTOs;
using Peep;
using Peep.Crawler.Models;

namespace Peep.Crawler.Application.Commands.QueueCrawl
{
    public class QueueCrawlRequest : IRequest<QueueCrawlResponseDTO>
    {
        public IdentifiableCrawlJob Job { get; set; }

        public QueueCrawlRequest(IdentifiableCrawlJob job)
        {
            Job = job;
        }
    }
}
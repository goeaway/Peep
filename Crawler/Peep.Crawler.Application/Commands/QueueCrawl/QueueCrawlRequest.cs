using MediatR;
using Peep.Crawler.Models.DTOs;
using Peep;

namespace Peep.Crawler.Application.Commands.QueueCrawl
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
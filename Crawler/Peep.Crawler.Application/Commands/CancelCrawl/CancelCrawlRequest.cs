using MediatR;
using Peep.Crawler.Models.DTOs;

namespace Peep.Crawler.Application.Commands.CancelQueuedCrawl
{
    public class CancelCrawlRequest : IRequest<CancelCrawlResponseDTO>
    {
        public string CrawlId { get; set; }

        public CancelCrawlRequest(string crawlId)
        {
            CrawlId = crawlId;
        }
    }
}
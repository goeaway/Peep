using MediatR;
using Peep.API.Models.DTOs;

namespace Peep.API.Application.Requests.Commands.CancelCrawl
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
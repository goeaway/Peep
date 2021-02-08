using MediatR;
using Peep.API.Models.DTOs;

namespace Peep.API.Application.Queries.GetCrawl
{
    public class GetCrawlRequest : IRequest<GetCrawlResponseDTO>
    {
        public string CrawlId { get; set; }

        public GetCrawlRequest(string crawlId)
        {
            CrawlId = crawlId;
        }
    }
}
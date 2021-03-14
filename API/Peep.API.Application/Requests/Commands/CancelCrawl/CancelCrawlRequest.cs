using MediatR;
using Peep.API.Models.DTOs;
using Peep.Core.API;

namespace Peep.API.Application.Requests.Commands.CancelCrawl
{
    public class CancelCrawlRequest : IRequest<Either<CancelCrawlResponseDto, HttpErrorResponse>>
    {
        public string CrawlId { get; set; }

        public CancelCrawlRequest(string crawlId)
        {
            CrawlId = crawlId;
        }
    }
}
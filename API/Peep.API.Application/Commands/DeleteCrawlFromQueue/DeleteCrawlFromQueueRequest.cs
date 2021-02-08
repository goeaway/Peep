using MediatR;
using Peep.API.Models.DTOs;

namespace Peep.API.Application.Commands.DeleteCrawlFromQueue
{
    public class DeleteCrawlFromQueueRequest : IRequest<DeleteCrawlFromQueueResponseDTO>
    {
        public string CrawlId { get; set; }
        public DeleteCrawlFromQueueRequest(string crawlId)
        {
            CrawlId = crawlId;
        }
    }
}
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Peep.Crawler.Application.Commands.CancelQueuedCrawl;
using Peep.Crawler.Application.Commands.QueueCrawl;
using Peep.Crawler.Models;
using Peep.Crawler.Models.DTOs;

namespace Peep.Crawler.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CrawlController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CrawlController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public Task<QueueCrawlResponseDTO> Queue(IdentifiableCrawlJob job)
        {
            return _mediator.Send(new QueueCrawlRequest(job));
        }

        [HttpDelete("{crawlId}")]
        public Task<CancelCrawlResponseDTO> Cancel(string crawlId)
        {
            return _mediator.Send(new CancelCrawlRequest(crawlId));
        }
    }
}

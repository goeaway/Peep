using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Peep.API.Application.Requests.Commands.CancelCrawl;
using Peep.API.Application.Requests.Commands.QueueCrawl;
using Peep.API.Application.Requests.Queries.GetCrawl;

namespace Peep.API.Controllers
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

        [HttpGet("{crawlId}")]
        public async Task<IActionResult> Get(string crawlId)
        {
            var result = await _mediator
                .Send(new GetCrawlRequest(crawlId));

            return result.Match(
                Ok,
                error => StatusCode((int)error.StatusCode, error)
            );
        }

        [HttpPost]
        public async Task<IActionResult> Queue(StoppableCrawlJob job)
        {
            var result = await _mediator.Send(new QueueCrawlRequest(job));

            return result.Match(
                Ok,
                error => StatusCode((int) error.StatusCode, error)
            );
        }

        [HttpPost("cancel/{crawlId}")]
        public async Task<IActionResult> Cancel(string crawlId)
        {
            var result = await _mediator.Send(new CancelCrawlRequest(crawlId));

            return result
                .Match(
                    Ok,
                    error => StatusCode((int)error.StatusCode, error)    
                );
        }
    }
}

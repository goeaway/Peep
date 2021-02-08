using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Peep.API.Application.Commands.DeleteCrawlFromQueue;
using Peep.API.Application.Queries.GetCrawlQueue;
using Peep.API.Models.DTOs;

namespace Peep.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QueueController : ControllerBase
    {
        private readonly IMediator _mediator;

        public QueueController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public Task<IEnumerable<QueuedCrawlDTO>> Get()
        {
            return _mediator.Send(new GetCrawlQueueRequest());
        }

        [HttpDelete("{crawlId}")]
        public Task<DeleteCrawlFromQueueResponseDTO> Delete(string crawlId)
        {
            // will return error if crawl not in queue (when crawl is running it is not in the queue)
            return _mediator.Send(new DeleteCrawlFromQueueRequest(crawlId));
        }
    }
}

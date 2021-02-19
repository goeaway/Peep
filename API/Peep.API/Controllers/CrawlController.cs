using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Peep.API.Application.Requests.Commands.CancelCrawl;
using Peep.API.Application.Requests.Commands.QueueCrawl;
using Peep.API.Application.Requests.Queries.GetCrawl;
using Peep.API.Models.DTOs;
using Peep.Core;

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
        public Task<GetCrawlResponseDTO> Get(string crawlId)
        {
            return _mediator.Send(new GetCrawlRequest(crawlId));
        }

        [HttpPost]
        public Task<QueueCrawlResponseDTO> Queue(StoppableCrawlJob job)
        {
            return _mediator.Send(new QueueCrawlRequest(job));
        }

        [HttpPost("cancel/{crawlId}")]
        public Task<CancelCrawlResponseDTO> Cancel(string crawlId)
        {
            return _mediator.Send(new CancelCrawlRequest(crawlId));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Peep.UrlFrontier.Application.Commands.Dequeue;
using Peep.UrlFrontier.Application.Commands.Enqueue;
using Peep.UrlFrontier.Dtos;

namespace Peep.UrlFrontier.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QueueController : ControllerBase
    {
        private IMediator _mediator;

        public QueueController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("dequeue")]
        public Task<Uri> Dequeue()
        {
            return _mediator.Send(new DequeueRequest());
        }

        [HttpPost("enqueue")]
        public Task Enqueue(QueueEnqueueRequestDto dto)
        {
            return _mediator.Send(new EnqueueRequest {Source = dto.Source, Uris = dto.Uris});
        }
    }
}
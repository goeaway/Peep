using MediatR;
using Peep.API.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Peep.API.Application.Commands.DeleteCrawlFromQueue
{
    public class DeleteCrawlFromQueueHandler : IRequestHandler<DeleteCrawlFromQueueRequest, DeleteCrawlFromQueueResponseDTO>
    {
        public Task<DeleteCrawlFromQueueResponseDTO> Handle(DeleteCrawlFromQueueRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

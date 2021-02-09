using MediatR;
using Peep.API.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Peep.API.Application.Commands.CancelQueuedCrawl
{
    public class CancelCrawlHandler : IRequestHandler<CancelCrawlRequest, CancelCrawlResponseDTO>
    {
        public Task<CancelCrawlResponseDTO> Handle(CancelCrawlRequest request, CancellationToken cancellationToken)
        {
            // try and find in the db, remove from there
            // if not found, try and signal to hosted service to cancel if it is running
            throw new NotImplementedException();
        }
    }
}

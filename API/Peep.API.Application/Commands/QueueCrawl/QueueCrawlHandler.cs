using MediatR;
using Peep.API.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Peep.API.Application.Commands.QueueCrawl
{
    public class QueueCrawlHandler : IRequestHandler<QueueCrawlRequest, QueueCrawlResponseDTO>
    {
        public Task<QueueCrawlResponseDTO> Handle(QueueCrawlRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

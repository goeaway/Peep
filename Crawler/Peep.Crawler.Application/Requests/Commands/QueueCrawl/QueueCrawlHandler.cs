using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Peep.Core.API;
using Peep.Crawler.Application.Services;

namespace Peep.Crawler.Application.Requests.Commands.QueueCrawl
{
    public class QueueCrawlHandler : IRequestHandler<QueueCrawlRequest, Either<Unit, ErrorResponseDTO>>
    {
        private readonly IJobQueue _jobQueue;
        
        public QueueCrawlHandler(IJobQueue jobQueue)
        {
            _jobQueue = jobQueue;
        }

        public Task<Either<Unit, ErrorResponseDTO>> Handle(QueueCrawlRequest request, CancellationToken cancellationToken)
        {
            _jobQueue.Enqueue(request.Job);
            return Task.FromResult(new Either<Unit, ErrorResponseDTO>(Unit.Value));
        }
    }
}

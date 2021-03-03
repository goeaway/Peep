using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Peep.Crawler.Application.Services;

namespace Peep.Crawler.Application.Requests.Commands.QueueCrawl
{
    public class QueueCrawlHandler : IRequestHandler<QueueCrawlRequest, Unit>
    {
        private readonly IJobQueue _jobQueue;
        
        public QueueCrawlHandler(IJobQueue jobQueue)
        {
            _jobQueue = jobQueue;
        }

        public Task<Unit> Handle(QueueCrawlRequest request, CancellationToken cancellationToken)
        {
            _jobQueue.Enqueue(request.Job);
            return Task.FromResult(Unit.Value);
        }
    }
}

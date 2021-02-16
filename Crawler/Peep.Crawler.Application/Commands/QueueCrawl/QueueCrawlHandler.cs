using MediatR;
using Newtonsoft.Json;
using Peep.Crawler.Models.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;
using Peep.Core.API.Providers;
using System.Collections;
using System.Collections.Generic;
using Peep.Crawler.Models;

namespace Peep.Crawler.Application.Commands.QueueCrawl
{
    public class QueueCrawlHandler : IRequestHandler<QueueCrawlRequest, QueueCrawlResponseDTO>
    {
        private readonly IJobQueue _jobQueue;

        public QueueCrawlHandler(IJobQueue jobQueue)
        {
            _jobQueue = jobQueue;
        }

        public Task<QueueCrawlResponseDTO> Handle(QueueCrawlRequest request, CancellationToken cancellationToken)
        {
            _jobQueue.Enqueue(request.Job);

            return Task.FromResult(new QueueCrawlResponseDTO());
        }
    }
}

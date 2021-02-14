using MediatR;
using Newtonsoft.Json;
using Peep.Crawler.Models.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;
using Peep.Core.API.Providers;

namespace Peep.Crawler.Application.Commands.QueueCrawl
{
    public class QueueCrawlHandler : IRequestHandler<QueueCrawlRequest, QueueCrawlResponseDTO>
    {
        private readonly INowProvider _nowProvider;

        public QueueCrawlHandler(INowProvider nowProvider)
        {
            _nowProvider = nowProvider;
        }

        public async Task<QueueCrawlResponseDTO> Handle(QueueCrawlRequest request, CancellationToken cancellationToken)
        {
            //var queuedJob = new QueuedJob
            //{
            //    JobJson = JsonConvert.SerializeObject(request.Job),
            //    DateQueued = _nowProvider.Now,
            //    Id = Guid.NewGuid().ToString()
            //};

            //_context.QueuedJobs.Add(queuedJob);

            //await _context.SaveChangesAsync();

            return new QueueCrawlResponseDTO
            {
                CrawlId = ""
            };
        }
    }
}

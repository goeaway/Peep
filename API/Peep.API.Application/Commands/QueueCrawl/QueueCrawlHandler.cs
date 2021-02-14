using MediatR;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Peep.API.Application.Providers;
using Peep.API.Models.DTOs;
using Peep.API.Models.Entities;
using Peep.API.Persistence;
using Peep.Core;
using Peep.Core.API.Providers;
using Peep.StopConditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Peep.API.Application.Commands.QueueCrawl
{
    public class QueueCrawlHandler : IRequestHandler<QueueCrawlRequest, QueueCrawlResponseDTO>
    {
        private readonly PeepApiContext _context;
        private readonly INowProvider _nowProvider;

        public QueueCrawlHandler(PeepApiContext context, INowProvider nowProvider)
        {
            _context = context;
            _nowProvider = nowProvider;
        }

        public async Task<QueueCrawlResponseDTO> Handle(QueueCrawlRequest request, CancellationToken cancellationToken)
        {
            // force the crawl to have some upper limit stop conditions
            if(request.Job.StopConditions == null)
            {
                request.Job.StopConditions = GetRequiredStopConditions();
            } 
            else
            {
                request.Job.StopConditions = request.Job.StopConditions.Concat(GetRequiredStopConditions());
            }

            var queuedJob = new QueuedJob
            {
                JobJson = JsonConvert.SerializeObject(request.Job),
                DateQueued = _nowProvider.Now,
                Id = Guid.NewGuid().ToString()
            };

            _context.QueuedJobs.Add(queuedJob);

            await _context.SaveChangesAsync();

            return new QueueCrawlResponseDTO
            {
                CrawlId = queuedJob.Id
            };
        }

        private IEnumerable<ICrawlStopCondition> GetRequiredStopConditions()
        {
            return new List<ICrawlStopCondition>
                {
                    new SerialisableStopCondition
                    {
                        Type = SerialisableStopConditionType.MaxDurationSeconds,
                        Value = TimeSpan.FromDays(1).TotalSeconds
                    },
                    new SerialisableStopCondition
                    {
                        Type = SerialisableStopConditionType.MaxCrawlCount,
                        Value = 1_000_000
                    }
                };
        }
    }
}

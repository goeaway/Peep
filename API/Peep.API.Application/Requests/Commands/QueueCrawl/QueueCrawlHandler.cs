using MediatR;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
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
using Peep.Core.API;

namespace Peep.API.Application.Requests.Commands.QueueCrawl
{
    public class QueueCrawlHandler : IRequestHandler<QueueCrawlRequest, Either<QueueCrawlResponseDto, ErrorResponseDTO>>
    {
        private readonly PeepApiContext _context;
        private readonly INowProvider _nowProvider;

        public QueueCrawlHandler(PeepApiContext context, INowProvider nowProvider)
        {
            _context = context;
            _nowProvider = nowProvider;
        }

        public async Task<Either<QueueCrawlResponseDto, ErrorResponseDTO>> Handle(QueueCrawlRequest request, CancellationToken cancellationToken)
        {
            // force the crawl to have some upper limit stop conditions
            request.Job.StopConditions = 
                request.Job.StopConditions == null 
                    ? GetRequiredStopConditions() 
                    : request.Job.StopConditions.Concat(GetRequiredStopConditions());

            var queuedJob = new QueuedJob
            {
                JobJson = JsonConvert.SerializeObject(request.Job),
                DateQueued = _nowProvider.Now,
                Id = Guid.NewGuid().ToString()
            };

            await _context.QueuedJobs.AddAsync(queuedJob, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return new QueueCrawlResponseDto
            {
                CrawlId = queuedJob.Id
            };
        }

        private static IEnumerable<ICrawlStopCondition> GetRequiredStopConditions()
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

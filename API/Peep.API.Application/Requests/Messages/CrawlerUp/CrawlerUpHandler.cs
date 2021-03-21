using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Peep.API.Models.Entities;
using Peep.API.Models.Enums;
using Peep.API.Persistence;
using Peep.Core;
using Peep.Core.API;
using Peep.Core.API.Providers;
using Peep.Core.Infrastructure.Messages;

namespace Peep.API.Application.Requests.Messages.CrawlerUp
{
    public class CrawlerUpHandler : IRequestHandler<CrawlerUpRequest, Either<Unit, MessageErrorResponse>>
    {
        private readonly PeepApiContext _context;
        private readonly INowProvider _nowProvider;
        private readonly ISendEndpointProvider _sendEndpointProvider;
        
        public CrawlerUpHandler(
            PeepApiContext context, 
            INowProvider nowProvider, 
            ISendEndpointProvider sendEndpointProvider)
        {
            _context = context;
            _nowProvider = nowProvider;
            _sendEndpointProvider = sendEndpointProvider;
        }

        public async Task<Either<Unit, MessageErrorResponse>> Handle(CrawlerUpRequest request, CancellationToken cancellationToken)
        {
            var found = await _context.JobCrawlers.FindAsync(request.CrawlerId);

            if (found != null)
            {
                return new MessageErrorResponse
                {
                    Message = $"Crawler with id {request.CrawlerId} already exists"
                };
            }

            await _context.JobCrawlers.AddAsync(new JobCrawler
            {
                CrawlerId = request.CrawlerId,
                LastHeartbeat = _nowProvider.Now
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            // if any job is running at the moment we should send a message directly back to this crawler's queue so it can help out
            var runningJob =
                await _context.Jobs.FirstOrDefaultAsync(j => j.State == JobState.Running, cancellationToken);
            
            if (runningJob != null)
            {
                var endpoint = await _sendEndpointProvider
                    .GetSendEndpoint(new Uri($"queue:crawl-queued-{request.CrawlerId}"));

                await endpoint.Send(new CrawlQueued
                {
                    Job = new IdentifiableCrawlJob(JsonConvert.DeserializeObject<CrawlJob>(runningJob.JobJson), runningJob.Id)
                }, cancellationToken);
            }
            
            return Unit.Value;
        }
    }
}
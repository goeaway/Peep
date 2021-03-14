using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Peep.API.Models.Entities;
using Peep.API.Persistence;
using Peep.Core.API;

namespace Peep.API.Application.Requests.Messages.PushCrawlData
{
    public class PushCrawlDataHandler : IRequestHandler<PushCrawlDataRequest, Either<Unit, HttpErrorResponse>>
    {
        private readonly PeepApiContext _context;

        public PushCrawlDataHandler(PeepApiContext context)
        {
            _context = context;
        }

        public async Task<Either<Unit, HttpErrorResponse>> Handle(PushCrawlDataRequest request, CancellationToken cancellationToken)
        {
            var foundJob = await _context.Jobs.FindAsync(request.JobId);

            if (foundJob == null)
            {
                return new HttpErrorResponse
                {
                    Message = "Could not find job"
                };
            }

            foundJob.JobData ??= new List<JobData>();

            foreach (var (key, value) in request.Data)
            {
                foundJob.JobData.AddRange(
                    value.Select(item => new JobData
                    {
                        Source = key.AbsoluteUri,
                        Value = item
                    }));                
            }

            await _context.SaveChangesAsync(cancellationToken);
            
            return Unit.Value;
        }
    }
}
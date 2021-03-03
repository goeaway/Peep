using MediatR;
using Peep.API.Models.Entities;

namespace Peep.API.Application.Requests.Commands.RunCrawl
{
    public class RunCrawlRequest : IRequest<Unit>
    {
        public QueuedJob Job { get; set; }
        public StoppableCrawlJob JobData { get; set; }
    }
}
using MediatR;
using Peep.API.Models.Entities;

namespace Peep.API.Application.Requests.Commands.RunCrawl
{
    public class RunCrawlRequest : IRequest<Unit>
    {
        public string JobId { get; set; }
        public StoppableCrawlJob JobActual { get; set; }
    }
}
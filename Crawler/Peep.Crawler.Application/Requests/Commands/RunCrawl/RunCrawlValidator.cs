using FluentValidation;

namespace Peep.Crawler.Application.Requests.Commands.RunCrawl
{
    public class RunCrawlValidator : AbstractValidator<RunCrawlRequest>
    {
        public RunCrawlValidator()
        {
        }
    }
}

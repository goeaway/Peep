using FluentValidation;

namespace Peep.Crawler.Application.Requests.Commands.CancelCrawl
{
    public class CancelCrawlValidator : AbstractValidator<CancelCrawlRequest>
    {
        public CancelCrawlValidator()
        {
        }
    }
}

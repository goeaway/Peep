using FluentValidation;

namespace Peep.API.Application.Requests.Commands.CancelCrawl
{
    public class CancelCrawlValidator : AbstractValidator<CancelCrawlRequest>
    {
        public CancelCrawlValidator()
        {
            RuleFor(x => x.CrawlId).NotEmpty().WithMessage("Crawl id required");
        }
    }
}

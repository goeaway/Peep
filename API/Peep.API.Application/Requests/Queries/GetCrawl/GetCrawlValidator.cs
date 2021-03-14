using FluentValidation;

namespace Peep.API.Application.Requests.Queries.GetCrawl
{
    public class GetCrawlValidator : AbstractValidator<GetCrawlRequest>
    {
        public GetCrawlValidator()
        {
            RuleFor(x => x.CrawlId).NotEmpty().WithMessage("Crawl id required");
        }
    }
}

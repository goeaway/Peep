using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Crawler.Application.Commands.CancelQueuedCrawl
{
    public class CancelCrawlValidator : AbstractValidator<CancelCrawlRequest>
    {
        public CancelCrawlValidator()
        {
            RuleFor(x => x.CrawlId).NotEmpty().WithMessage("Crawl id required");
        }
    }
}

using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.API.Application.Requests.Commands.QueueCrawl
{
    public class QueueCrawlValidator : AbstractValidator<QueueCrawlRequest>
    {
        public QueueCrawlValidator()
        {
            RuleFor(x => x.Job)
                .NotEmpty().WithMessage("Job required");

            When(x => x.Job != null, () =>
            {
                RuleFor(x => x.Job.Seeds)
                    .NotEmpty()
                    .WithMessage("At least 1 seed uri is required");
            });
        }
    }
}

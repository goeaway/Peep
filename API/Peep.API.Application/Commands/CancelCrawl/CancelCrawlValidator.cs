using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.API.Application.Commands.CancelQueuedCrawl
{
    public class CancelCrawlValidator : AbstractValidator<CancelCrawlRequest>
    {
        public CancelCrawlValidator()
        {

        }
    }
}

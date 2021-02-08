using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.API.Application.Commands.QueueCrawl
{
    public class QueueCrawlValidator : AbstractValidator<QueueCrawlRequest>
    {
        public QueueCrawlValidator()
        {

        }
    }
}

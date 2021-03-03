using FluentValidation;

namespace Peep.Crawler.Application.Requests.Commands.QueueCrawl
{
    public class QueueCrawlValidator : AbstractValidator<QueueCrawlRequest>
    {
        public QueueCrawlValidator()
        {
        }
    }
}

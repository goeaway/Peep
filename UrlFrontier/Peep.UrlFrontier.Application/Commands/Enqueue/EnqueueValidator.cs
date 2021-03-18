using FluentValidation;

namespace Peep.UrlFrontier.Application.Commands.Enqueue
{
    public class EnqueueValidator : AbstractValidator<EnqueueRequest>
    {
        public EnqueueValidator()
        {
            RuleFor(x => x.Source).NotEmpty().WithMessage("Source uri required");

            RuleFor(x => x.Uris).NotNull().WithMessage("Uris array required");
        }
    }
}
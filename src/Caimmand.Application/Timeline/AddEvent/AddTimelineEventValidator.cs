using FluentValidation;

namespace Caimmand.Application.Timeline.AddEvent;

public sealed class AddTimelineEventValidator : AbstractValidator<AddTimelineEventCommand>
{
    public AddTimelineEventValidator()
    {
        RuleFor(x => x.Type).NotEmpty();
        RuleFor(x => x.Origin).NotEmpty();
        RuleFor(x => x.Content).NotEmpty();
    }
}
using Caimmand.Domain.Enums;
using FluentValidation;

namespace Caimmand.Application.Participants.Register;

public sealed class RegisterParticipantValidator : AbstractValidator<RegisterParticipantCommand>
{
    public RegisterParticipantValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Reference).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ExternalId).MaximumLength(200);
        RuleFor(x => x.Rol).NotEmpty().MaximumLength(100);
    }
}
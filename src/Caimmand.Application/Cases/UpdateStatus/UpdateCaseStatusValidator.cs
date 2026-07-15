using FluentValidation;

namespace Caimmand.Application.Cases.UpdateStatus;

public sealed class UpdateCaseStatusValidator : AbstractValidator<UpdateCaseStatusCommand>
{
    public UpdateCaseStatusValidator()
    {
        RuleFor(x => x.NewStatus).IsInEnum();
    }
}
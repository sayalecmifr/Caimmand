using FluentValidation;

namespace Caimmand.Application.Tasks.Create;

public sealed class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.Type).NotEmpty().MaximumLength(100);
    }
}
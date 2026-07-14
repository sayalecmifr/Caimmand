using System.Text.Json;
using FluentValidation;

namespace Caimmand.Application.Cases.Create;

public sealed class CreateCaseValidator : AbstractValidator<CreateCaseCommand>
{
    public CreateCaseValidator()
    {
        RuleFor(x => x.CaseDefinitionCode).NotEmpty();
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.SourceSystem).NotEmpty();
        RuleFor(x => x.Context)
            .Must(c => c.ValueKind != JsonValueKind.Undefined)
            .WithMessage("Context es obligatorio.");
    }
}
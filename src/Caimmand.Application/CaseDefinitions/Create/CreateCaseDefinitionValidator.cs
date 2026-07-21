using FluentValidation;

namespace Caimmand.Application.CaseDefinitions.Create;

public sealed class CreateCaseDefinitionValidator : AbstractValidator<CreateCaseDefinitionCommand>
{
    private static readonly string[] Priorities = ["Baja", "Media", "Alta", "Urgente"];

    public CreateCaseDefinitionValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.DefaultPriority)
            .NotEmpty()
            .Must(p => Priorities.Contains(p))
            .WithMessage("DefaultPriority debe ser uno de: Baja, Media, Alta, Urgente.");
        RuleFor(x => x.DisplayColor)
            .NotEmpty()
            .Matches("^#[0-9a-fA-F]{6}$")
            .WithMessage("DisplayColor debe ser un color hex (#RRGGBB).");
        RuleFor(x => x.DisplayIcon)
            .NotEmpty();
    }
}
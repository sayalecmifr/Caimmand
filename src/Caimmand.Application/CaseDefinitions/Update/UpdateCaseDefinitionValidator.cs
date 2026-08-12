using FluentValidation;

namespace Caimmand.Application.CaseDefinitions.Update;

public sealed class UpdateCaseDefinitionValidator : AbstractValidator<UpdateCaseDefinitionCommand>
{
    private static readonly string[] Priorities = ["Baja", "Media", "Alta", "Urgente"];

    public UpdateCaseDefinitionValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.DefaultPriority)
            .NotEmpty()
            .Must(p => Priorities.Contains(p))
            .WithMessage("DefaultPriority debe ser uno de: Baja, Media, Alta, Urgente.");
        RuleFor(x => x.DisplayColor)
            .NotEmpty()
            .Matches("^#[0-9a-fA-F]{6}$")
            .WithMessage("DisplayColor debe ser un color hex (#RRGGBB).");
        RuleFor(x => x.DisplayIcon).NotEmpty().MaximumLength(50);
    }
}
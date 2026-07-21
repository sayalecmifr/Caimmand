namespace Caimmand.Application.CaseDefinitions.Create;

public sealed record CreateCaseDefinitionCommand(
    string Code,
    string Name,
    string Description,
    string? Category,
    string DefaultPriority,
    string DisplayColor,
    string DisplayIcon);
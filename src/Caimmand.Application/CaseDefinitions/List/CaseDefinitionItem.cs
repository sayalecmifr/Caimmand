namespace Caimmand.Application.CaseDefinitions.List;

public sealed record CaseDefinitionItem(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string? Category,
    bool IsActive,
    string? DefaultSla,
    string DefaultPriority,
    string DisplayColor,
    string DisplayIcon);
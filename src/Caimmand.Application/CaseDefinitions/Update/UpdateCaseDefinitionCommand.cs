using Caimmand.Domain.Enums;

namespace Caimmand.Application.CaseDefinitions.Update;

public sealed record UpdateCaseDefinitionCommand(
    Guid Id,
    string Name,
    string Description,
    string? Category,
    string DefaultPriority,
    string DisplayColor,
    string DisplayIcon,
    List<CaseStatus>? AllowedStatuses = null);
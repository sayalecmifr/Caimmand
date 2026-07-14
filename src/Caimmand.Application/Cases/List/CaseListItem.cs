using Caimmand.Domain.Enums;

namespace Caimmand.Application.Cases.List;

public sealed record CaseListItem(
    Guid Id,
    string Title,
    string CaseDefinitionCode,
    string CaseDefinitionName,
    CaseStatus Status,
    string SourceSystem,
    DateTime CreatedAt);
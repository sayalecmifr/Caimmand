using Caimmand.Domain.Enums;

namespace Caimmand.Application.Cases.List;

public sealed record ListCasesQuery(CaseStatus? Status, string? CaseDefinitionCode);
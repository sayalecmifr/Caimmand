using Caimmand.Domain.Enums;

namespace Caimmand.Application.Cases.List;

public sealed record ListCasesQuery(
    CaseStatus? Status,
    string? CaseDefinitionCode,
    string? ExternalId,
    DateTime? CreatedFrom,
    DateTime? CreatedTo,
    DateTime? UpdatedFrom,
    DateTime? UpdatedTo,
    int Page = 1,
    int PageSize = 50);
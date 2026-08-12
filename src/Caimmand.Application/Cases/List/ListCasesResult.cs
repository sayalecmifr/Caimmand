namespace Caimmand.Application.Cases.List;

public sealed record ListCasesResult(
    IReadOnlyList<CaseListItem> Items,
    int Total,
    int Page,
    int PageSize,
    int TotalPages);
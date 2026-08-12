using Caimmand.Domain;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Application.Cases.List;

public sealed class ListCasesHandler
{
    private readonly ICaimmandDbContext _db;
    private readonly IJsonQueryAdapter _jsonAdapter;

    public ListCasesHandler(ICaimmandDbContext db, IJsonQueryAdapter jsonAdapter)
    {
        _db = db;
        _jsonAdapter = jsonAdapter;
    }

    public async Task<ListCasesResult> Handle(ListCasesQuery query, CancellationToken ct)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 50 : query.PageSize;

        var createdFrom = NormalizeUtc(query.CreatedFrom);
        var createdTo = NormalizeUtc(query.CreatedTo);
        var updatedFrom = NormalizeUtc(query.UpdatedFrom);
        var updatedTo = NormalizeUtc(query.UpdatedTo);

        var queryable = _db.Cases
            .Where(c => query.Status == null || c.Status == query.Status.Value)
            .Where(c => string.IsNullOrEmpty(query.CaseDefinitionCode) || c.CaseDefinitionCode == query.CaseDefinitionCode)
            .Where(c => createdFrom == null || c.CreatedAt >= createdFrom.Value)
            .Where(c => createdTo == null || c.CreatedAt <= createdTo.Value)
            .Where(c => updatedFrom == null || c.UpdatedAt >= updatedFrom.Value)
            .Where(c => updatedTo == null || c.UpdatedAt <= updatedTo.Value);

        if (!string.IsNullOrEmpty(query.ExternalId))
        {
            queryable = _jsonAdapter.WhereExternalId(queryable, query.ExternalId);
        }

        var total = await queryable.CountAsync(ct);

        var cases = await queryable
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var definitions = await _db.CaseDefinitions
            .ToDictionaryAsync(d => d.Code, d => d.Name, ct);

        var items = cases
            .Select(c => new CaseListItem(
                c.Id,
                c.Title,
                c.CaseDefinitionCode,
                definitions.GetValueOrDefault(c.CaseDefinitionCode, c.CaseDefinitionCode),
                c.Status,
                c.SourceSystem,
                c.CreatedAt))
            .ToList();

        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

        return new ListCasesResult(items, total, page, pageSize, totalPages);
    }

    private static DateTime? NormalizeUtc(DateTime? value) =>
        value is null ? null : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
}
using Caimmand.Domain;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Application.Cases.List;

public sealed class ListCasesHandler
{
    private readonly ICaimmandDbContext _db;

    public ListCasesHandler(ICaimmandDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CaseListItem>> Handle(ListCasesQuery query, CancellationToken ct)
    {
        var cases = await _db.Cases
            .Where(c => query.Status == null || c.Status == query.Status.Value)
            .Where(c => string.IsNullOrEmpty(query.CaseDefinitionCode) || c.CaseDefinitionCode == query.CaseDefinitionCode)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        var definitions = await _db.CaseDefinitions
            .ToDictionaryAsync(d => d.Code, d => d.Name, ct);

        return cases
            .Select(c => new CaseListItem(
                c.Id,
                c.Title,
                c.CaseDefinitionCode,
                definitions.GetValueOrDefault(c.CaseDefinitionCode, c.CaseDefinitionCode),
                c.Status,
                c.SourceSystem,
                c.CreatedAt))
            .ToList();
    }
}
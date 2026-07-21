using Caimmand.Domain;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Application.CaseDefinitions.List;

public sealed class ListCaseDefinitionsHandler
{
    private readonly ICaimmandDbContext _db;

    public ListCaseDefinitionsHandler(ICaimmandDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CaseDefinitionItem>> Handle(ListCaseDefinitionsQuery query, CancellationToken ct)
    {
        var definitions = await _db.CaseDefinitions
            .OrderBy(d => d.Name)
            .ToListAsync(ct);

        return definitions
            .Select(d => new CaseDefinitionItem(
                d.Id,
                d.Code,
                d.Name,
                d.Description,
                d.Category,
                d.IsActive,
                d.DefaultSla?.ToString(),
                d.DefaultPriority,
                d.DisplayColor,
                d.DisplayIcon))
            .ToList();
    }
}
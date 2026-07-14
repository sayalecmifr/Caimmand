using Caimmand.Domain;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Application.Cases.GetDetail;

public sealed class GetCaseDetailHandler
{
    private readonly ICaimmandDbContext _db;

    public GetCaseDetailHandler(ICaimmandDbContext db)
    {
        _db = db;
    }

    public async Task<CaseDetail?> Handle(GetCaseDetailQuery query, CancellationToken ct)
    {
        var entity = await _db.Cases
            .FirstOrDefaultAsync(c => c.Id == query.Id, ct);

        if (entity is null)
        {
            return null;
        }

        var definition = await _db.CaseDefinitions
            .FirstOrDefaultAsync(d => d.Code == entity.CaseDefinitionCode, ct);

        return new CaseDetail(
            entity.Id,
            entity.Title,
            entity.CaseDefinitionCode,
            definition?.Name ?? entity.CaseDefinitionCode,
            entity.Status,
            entity.SourceSystem,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.Context);
    }
}
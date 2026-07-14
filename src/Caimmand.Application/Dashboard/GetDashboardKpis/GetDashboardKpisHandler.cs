using Caimmand.Domain;
using Caimmand.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Application.Dashboard.GetDashboardKpis;

public sealed class GetDashboardKpisHandler
{
    private readonly ICaimmandDbContext _db;

    public GetDashboardKpisHandler(ICaimmandDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardKpis> Handle(GetDashboardKpisQuery query, CancellationToken ct)
    {
        var total = await _db.Cases.CountAsync(ct);
        var created = await _db.Cases.CountAsync(c => c.Status == CaseStatus.Creado, ct);
        var finalizados = await _db.Cases.CountAsync(c => c.Status == CaseStatus.Finalizado, ct);
        var requierenIntervencion = await _db.Cases.CountAsync(c => c.Status == CaseStatus.Suspendido, ct);

        return new DashboardKpis(total, created, finalizados, requierenIntervencion);
    }
}
using Caimmand.Domain;
using Caimmand.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;
using TaskEntity = Caimmand.Domain.Entities.Task;
using TaskStatus = Caimmand.Domain.Enums.TaskStatus;

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

        var now = DateTime.UtcNow;
        var tasksOverdue = await _db.Tasks
            .CountAsync(t =>
                (t.Status == TaskStatus.Pendiente || t.Status == TaskStatus.EnProgreso)
                && t.DueAt != null
                && t.DueAt < now, ct);

        return new DashboardKpis(total, created, finalizados, requierenIntervencion, tasksOverdue);
    }
}
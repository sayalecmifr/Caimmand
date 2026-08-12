using Caimmand.Domain;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;
using TaskStatus = Caimmand.Domain.Enums.TaskStatus;

namespace Caimmand.Application.Tasks.List;

public sealed class ListTasksHandler
{
    private readonly ICaimmandDbContext _db;

    public ListTasksHandler(ICaimmandDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TaskItem>> Handle(ListTasksQuery query, CancellationToken ct)
    {
        var tasks = await _db.Tasks
            .Where(t => t.CaseId == query.CaseId)
            .Where(t => query.Status == null || t.Status == query.Status.Value)
            .Where(t => query.AssigneeId == null || t.AssigneeId == query.AssigneeId.Value)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        return tasks
            .Select(t => new TaskItem(
                t.Id,
                t.CaseId,
                t.Type,
                t.Status.ToString(),
                t.AssigneeId,
                t.Result,
                t.CreatedAt,
                t.StartedAt,
                t.CompletedAt,
                t.DueAt))
            .ToList();
    }
}
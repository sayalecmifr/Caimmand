using Caimmand.Domain;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Application.Tasks.GetDetail;

public sealed class GetTaskHandler
{
    private readonly ICaimmandDbContext _db;

    public GetTaskHandler(ICaimmandDbContext db)
    {
        _db = db;
    }

    public async Task<TaskDetail?> Handle(GetTaskQuery query, CancellationToken ct)
    {
        var t = await _db.Tasks
            .FirstOrDefaultAsync(x => x.Id == query.TaskId && x.CaseId == query.CaseId, ct);

        if (t is null)
        {
            return null;
        }

        return new TaskDetail(
            t.Id,
            t.CaseId,
            t.Type,
            t.Status.ToString(),
            t.AssigneeId,
            t.Result,
            t.CreatedAt,
            t.StartedAt,
            t.CompletedAt,
            t.DueAt);
    }
}
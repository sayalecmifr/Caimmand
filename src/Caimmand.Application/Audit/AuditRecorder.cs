using Caimmand.Domain;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using Task = System.Threading.Tasks.Task;

namespace Caimmand.Application.Audit;

public sealed class AuditRecorder : IAuditRecorder
{
    private readonly ICaimmandDbContext _db;

    public AuditRecorder(ICaimmandDbContext db)
    {
        _db = db;
    }

    public async Task RecordAsync(
        Guid caseId,
        AuditOperation operation,
        string origin,
        string changeJson,
        string? contextRef,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(origin))
        {
            throw new ArgumentException("Origin es obligatorio para AuditRecord.", nameof(origin));
        }
        if (string.IsNullOrWhiteSpace(changeJson))
        {
            changeJson = "{}";
        }

        _db.AuditRecords.Add(new AuditRecord
        {
            CaseId = caseId,
            Operation = operation,
            Origin = origin,
            OccurredAt = DateTime.UtcNow,
            ChangeJson = changeJson,
            ContextRef = contextRef
        });

        await Task.CompletedTask;
    }
}
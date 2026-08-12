using Caimmand.Domain.Enums;

namespace Caimmand.Application.Audit;

public interface IAuditRecorder
{
    Task RecordAsync(
        Guid caseId,
        AuditOperation operation,
        string origin,
        string changeJson,
        string? contextRef,
        CancellationToken ct);
}
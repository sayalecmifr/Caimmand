using Caimmand.Domain.Enums;

namespace Caimmand.Application.Audit.GetAudit;

public sealed record AuditRecordItem(
    Guid Id,
    Guid CaseId,
    string Operation,
    string Origin,
    DateTime OccurredAt,
    string ChangeJson,
    string? ContextRef);
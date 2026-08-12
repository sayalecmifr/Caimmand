using Caimmand.Domain.Enums;

namespace Caimmand.Domain.Entities;

public class AuditRecord
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    public AuditOperation Operation { get; set; }
    public string Origin { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string ChangeJson { get; set; } = "{}";
    public string? ContextRef { get; set; }
}
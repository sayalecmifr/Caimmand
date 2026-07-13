namespace Caimmand.Domain.Entities;

public class TimelineEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    public long Sequence { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
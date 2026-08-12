using TaskStatus = Caimmand.Domain.Enums.TaskStatus;

namespace Caimmand.Domain.Entities;

public class Task
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid? AssigneeId { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Pendiente;
    public string? Result { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? DueAt { get; set; }

    public Case? Case { get; set; }
    public Participant? Assignee { get; set; }
}
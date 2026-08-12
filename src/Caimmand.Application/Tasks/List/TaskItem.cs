namespace Caimmand.Application.Tasks.List;

public sealed record TaskItem(
    Guid Id,
    Guid CaseId,
    string Type,
    string Status,
    Guid? AssigneeId,
    string? Result,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime? DueAt);
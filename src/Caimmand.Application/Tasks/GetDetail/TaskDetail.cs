namespace Caimmand.Application.Tasks.GetDetail;

public sealed record TaskDetail(
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
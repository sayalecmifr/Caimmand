namespace Caimmand.Application.Tasks.Create;

public sealed record CreateTaskCommand(
    Guid CaseId,
    string Type,
    Guid? AssigneeId = null,
    DateTime? DueAt = null);
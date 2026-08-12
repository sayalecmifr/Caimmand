namespace Caimmand.Application.Tasks.Assign;

public sealed record AssignTaskCommand(Guid CaseId, Guid TaskId, Guid AssigneeId);
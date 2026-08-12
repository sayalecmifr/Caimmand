namespace Caimmand.Application.Tasks.Assign;

public sealed record AssignTaskResponse(Guid Id, Guid CaseId, Guid AssigneeId, string Status);
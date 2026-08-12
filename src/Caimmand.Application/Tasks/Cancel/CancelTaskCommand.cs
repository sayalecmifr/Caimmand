namespace Caimmand.Application.Tasks.Cancel;

public sealed record CancelTaskCommand(Guid CaseId, Guid TaskId);
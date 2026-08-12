namespace Caimmand.Application.Tasks.Start;

public sealed record StartTaskCommand(Guid CaseId, Guid TaskId);
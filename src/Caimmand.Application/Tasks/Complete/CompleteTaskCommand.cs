namespace Caimmand.Application.Tasks.Complete;

public sealed record CompleteTaskCommand(Guid CaseId, Guid TaskId, string? Result);
namespace Caimmand.Application.Tasks.Complete;

public sealed record CompleteTaskResponse(Guid Id, Guid CaseId, string Status, DateTime? CompletedAt, string? Result);
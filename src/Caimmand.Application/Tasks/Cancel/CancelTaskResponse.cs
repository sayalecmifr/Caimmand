namespace Caimmand.Application.Tasks.Cancel;

public sealed record CancelTaskResponse(Guid Id, Guid CaseId, string Status, DateTime? CompletedAt);
namespace Caimmand.Application.Tasks.Start;

public sealed record StartTaskResponse(Guid Id, Guid CaseId, string Status, DateTime? StartedAt);
namespace Caimmand.Application.Cases.UpdateStatus;

public sealed record UpdateCaseStatusResponse(Guid Id, string Status, DateTime UpdatedAt);
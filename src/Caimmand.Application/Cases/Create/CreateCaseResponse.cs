namespace Caimmand.Application.Cases.Create;

public sealed record CreateCaseResponse(Guid Id, string Status, DateTime CreatedAt);
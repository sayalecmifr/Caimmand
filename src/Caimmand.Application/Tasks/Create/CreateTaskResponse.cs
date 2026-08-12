using Caimmand.Domain.Enums;

namespace Caimmand.Application.Tasks.Create;

public sealed record CreateTaskResponse(
    Guid Id,
    Guid CaseId,
    string Type,
    string Status,
    Guid? AssigneeId,
    DateTime CreatedAt);
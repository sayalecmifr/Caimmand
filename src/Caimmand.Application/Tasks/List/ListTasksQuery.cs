using TaskStatus = Caimmand.Domain.Enums.TaskStatus;

namespace Caimmand.Application.Tasks.List;

public sealed record ListTasksQuery(
    Guid CaseId,
    TaskStatus? Status = null,
    Guid? AssigneeId = null);
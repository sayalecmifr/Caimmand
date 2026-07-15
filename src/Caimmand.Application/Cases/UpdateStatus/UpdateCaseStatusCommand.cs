using Caimmand.Domain.Enums;

namespace Caimmand.Application.Cases.UpdateStatus;

public sealed record UpdateCaseStatusCommand(Guid Id, CaseStatus NewStatus);
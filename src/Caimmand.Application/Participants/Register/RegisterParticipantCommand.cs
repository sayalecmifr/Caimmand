using Caimmand.Domain.Enums;

namespace Caimmand.Application.Participants.Register;

public sealed record RegisterParticipantCommand(
    Guid CaseId,
    ParticipantType Type,
    string Reference,
    string? ExternalId,
    string Rol);
using Caimmand.Domain.Enums;

namespace Caimmand.Application.Participants.List;

public sealed record ParticipantItem(
    Guid ParticipantId,
    string Type,
    string Reference,
    string? ExternalId,
    string Rol);
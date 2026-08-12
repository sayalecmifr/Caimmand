namespace Caimmand.Application.Participants.Register;

public sealed record RegisterParticipantResponse(Guid ParticipantId, Guid CaseId, string Rol);
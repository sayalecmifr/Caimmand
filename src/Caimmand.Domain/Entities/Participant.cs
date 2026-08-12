using Caimmand.Domain.Enums;

namespace Caimmand.Domain.Entities;

public class Participant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ParticipantType Type { get; set; } = ParticipantType.SistemaExterno;
    public string Reference { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
}
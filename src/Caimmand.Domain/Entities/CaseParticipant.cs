namespace Caimmand.Domain.Entities;

public class CaseParticipant
{
    public Guid CaseId { get; set; }
    public Guid ParticipantId { get; set; }
    public string Rol { get; set; } = string.Empty;

    public Case? Case { get; set; }
    public Participant? Participant { get; set; }
}
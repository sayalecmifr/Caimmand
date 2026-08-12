namespace Caimmand.Domain.Enums;

public enum AuditOperation
{
    CaseCreation,
    StatusChange,
    EventAdded,
    ParticipantRegistered,
    TaskCreated,
    TaskAssigned,
    TaskStarted,
    TaskCompleted,
    TaskCancelled
}
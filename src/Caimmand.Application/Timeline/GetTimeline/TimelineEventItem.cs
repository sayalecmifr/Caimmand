namespace Caimmand.Application.Timeline.GetTimeline;

public sealed record TimelineEventItem(
    Guid Id,
    long Sequence,
    string Type,
    string Origin,
    Guid? ParticipantId,
    string Content,
    DateTime OccurredAt);
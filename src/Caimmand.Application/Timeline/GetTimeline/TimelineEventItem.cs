namespace Caimmand.Application.Timeline.GetTimeline;

public sealed record TimelineEventItem(
    Guid Id,
    long Sequence,
    string Type,
    string Origin,
    string Content,
    DateTime OccurredAt);
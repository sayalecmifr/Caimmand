namespace Caimmand.Application.Timeline.AddEvent;

public sealed record AddTimelineEventResponse(Guid Id, long Sequence, DateTime OccurredAt);
namespace Caimmand.Application.Timeline.AddEvent;

public sealed record AddTimelineEventCommand(Guid CaseId, string Type, string Origin, string Content);
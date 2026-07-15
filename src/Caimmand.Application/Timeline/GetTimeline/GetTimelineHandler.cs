using Caimmand.Domain;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Application.Timeline.GetTimeline;

public sealed class GetTimelineHandler
{
    private readonly ICaimmandDbContext _db;

    public GetTimelineHandler(ICaimmandDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TimelineEventItem>> Handle(GetTimelineQuery query, CancellationToken ct)
    {
        var events = await _db.TimelineEvents
            .Where(e => e.CaseId == query.CaseId)
            .OrderByDescending(e => e.Sequence)
            .ToListAsync(ct);

        return events
            .Select(e => new TimelineEventItem(e.Id, e.Sequence, e.Type, e.Origin, e.Content, e.OccurredAt))
            .ToList();
    }
}
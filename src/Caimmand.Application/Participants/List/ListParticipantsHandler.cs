using Caimmand.Domain;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Application.Participants.List;

public sealed class ListParticipantsHandler
{
    private readonly ICaimmandDbContext _db;

    public ListParticipantsHandler(ICaimmandDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ParticipantItem>> Handle(ListParticipantsQuery query, CancellationToken ct)
    {
        var rows = await (
            from cp in _db.CaseParticipants
            join p in _db.Participants on cp.ParticipantId equals p.Id
            where cp.CaseId == query.CaseId
            orderby p.Reference
            select new { p, cp.Rol }
        ).ToListAsync(ct);

        return rows
            .Select(r => new ParticipantItem(
                r.p.Id,
                r.p.Type.ToString(),
                r.p.Reference,
                r.p.ExternalId,
                r.Rol))
            .ToList();
    }
}
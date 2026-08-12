using Caimmand.Application.Authorization;
using Caimmand.Domain;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace Caimmand.Application.Audit.GetAudit;

public sealed class GetAuditHandler
{
    private readonly ICaimmandDbContext _db;
    private readonly IAuthorizationContext _authorization;

    public GetAuditHandler(ICaimmandDbContext db, IAuthorizationContext authorization)
    {
        _db = db;
        _authorization = authorization;
    }

    public async Task<IReadOnlyList<AuditRecordItem>> Handle(GetAuditQuery query, CancellationToken ct)
    {
        if (!_authorization.IsInRole(Roles.Gerente))
        {
            throw new UnauthorizedOperationException(Roles.Gerente, _authorization.GetCurrentRole() ?? "(ninguno)");
        }

        var records = await _db.AuditRecords
            .Where(r => r.CaseId == query.CaseId)
            .OrderByDescending(r => r.OccurredAt)
            .ToListAsync(ct);

        return records
            .Select(r => new AuditRecordItem(
                r.Id,
                r.CaseId,
                r.Operation.ToString(),
                r.Origin,
                r.OccurredAt,
                r.ChangeJson,
                r.ContextRef))
            .ToList();
    }
}
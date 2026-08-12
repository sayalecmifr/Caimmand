using System.Text.Json;
using Caimmand.Application.Audit.GetAudit;
using Caimmand.Application.Authorization;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using Caimmand.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace Caimmand.Tests.Audit.GetAudit;

public class GetAuditHandlerTests
{
    private static readonly JsonElement Context =
        JsonDocument.Parse("""{"patientId":1}""").RootElement.Clone();

    private static GetAuditHandler BuildHandler(TestDbContext db, IAuthorizationContext? auth = null) =>
        new(db, auth ?? TestAuthorizationContext.AsGerente());

    [Fact]
    public async Task GetAudit_ReturnsRecords_ForCase_DescendingByOccurredAt()
    {
        using var db = TestDbContext.Create();
        db.CaseDefinitions.Add(new CaseDefinition { Code = "X", Name = "X", IsActive = true });
        var caseId = Guid.NewGuid();
        db.Cases.Add(new Case
        {
            Id = caseId,
            Title = "Caso",
            CaseDefinitionCode = "X",
            Status = CaseStatus.Creado,
            SourceSystem = "HIS",
            Context = JsonDocument.Parse("{}"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.AuditRecords.Add(new AuditRecord
        {
            CaseId = caseId,
            Operation = AuditOperation.CaseCreation,
            Origin = "HIS",
            OccurredAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            ChangeJson = "{}"
        });
        db.AuditRecords.Add(new AuditRecord
        {
            CaseId = caseId,
            Operation = AuditOperation.StatusChange,
            Origin = "Operador",
            OccurredAt = new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc),
            ChangeJson = "{\"from\":\"Creado\",\"to\":\"EnCurso\"}"
        });
        db.AuditRecords.Add(new AuditRecord
        {
            CaseId = Guid.NewGuid(),
            Operation = AuditOperation.CaseCreation,
            Origin = "HIS",
            OccurredAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            ChangeJson = "{}"
        });
        await db.SaveChangesAsync();

        var handler = BuildHandler(db);
        var result = await handler.Handle(new GetAuditQuery(caseId), default);

        Assert.Equal(2, result.Count);
        Assert.Equal("StatusChange", result[0].Operation);
        Assert.Equal("CaseCreation", result[1].Operation);
    }

    [Fact]
    public async Task GetAudit_NoRecords_ReturnsEmpty()
    {
        using var db = TestDbContext.Create();
        var handler = BuildHandler(db);

var result = await handler.Handle(new GetAuditQuery(Guid.NewGuid()), default);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAudit_AsOperador_ThrowsUnauthorized()
    {
        using var db = TestDbContext.Create();
        var handler = BuildHandler(db, TestAuthorizationContext.AsOperador());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() =>
            handler.Handle(new GetAuditQuery(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task GetAudit_AsSupervisor_ThrowsUnauthorized()
    {
        using var db = TestDbContext.Create();
        var handler = BuildHandler(db, TestAuthorizationContext.AsSupervisor());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() =>
            handler.Handle(new GetAuditQuery(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task GetAudit_AsGerente_Succeeds()
    {
        using var db = TestDbContext.Create();
        var handler = BuildHandler(db, TestAuthorizationContext.AsGerente());

        var result = await handler.Handle(new GetAuditQuery(Guid.NewGuid()), default);

        Assert.Empty(result);
    }
}



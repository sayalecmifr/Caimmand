using System.Text.Json;
using Caimmand.Application.Audit;
using Caimmand.Application.Authorization;
using Caimmand.Application.Cases.UpdateStatus;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using Caimmand.Domain.Exceptions;
using Caimmand.Tests.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace Caimmand.Tests.Cases.UpdateStatus;

public class UpdateCaseStatusHandlerTests
{
    private static readonly JsonElement Context =
        JsonDocument.Parse("""{"patientId":1}""").RootElement.Clone();

    private static async Task<(TestDbContext db, Guid caseId, Guid definitionId)> SeedCaseAsync(
        CaseStatus status = CaseStatus.Creado,
        List<CaseStatus>? allowedStatuses = null)
    {
        var db = TestDbContext.Create();
        var def = new CaseDefinition { Code = "X", Name = "X", IsActive = true, AllowedStatuses = allowedStatuses ?? new() };
        db.CaseDefinitions.Add(def);
        var entity = new Case
        {
            Id = Guid.NewGuid(),
            Title = "Caso",
            CaseDefinitionCode = "X",
            Status = status,
            SourceSystem = "HIS",
            Context = JsonDocument.Parse("{}"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Cases.Add(entity);
        await db.SaveChangesAsync();
        return (db, entity.Id, def.Id);
    }

    private static UpdateCaseStatusHandler BuildHandler(TestDbContext db, IAuthorizationContext? auth = null) =>
        new(db, new UpdateCaseStatusValidator(), new AuditRecorder(db), auth ?? TestAuthorizationContext.AsSupervisor());

    [Fact]
    public async Task UpdateStatus_CreadoToEnCurso_Succeeds()
    {
        var (db, caseId, _) = await SeedCaseAsync(CaseStatus.Creado);
        var handler = BuildHandler(db);

        var response = await handler.Handle(new UpdateCaseStatusCommand(caseId, CaseStatus.EnCurso), default);

        Assert.Equal(CaseStatus.EnCurso.ToString(), response.Status);
        var entity = await db.Cases.FirstAsync();
        Assert.Equal(CaseStatus.EnCurso, entity.Status);
    }

    [Fact]
    public async Task UpdateStatus_EnCursoToFinalizado_Succeeds()
    {
        var (db, caseId, _) = await SeedCaseAsync(CaseStatus.EnCurso);
        var handler = BuildHandler(db);

        var response = await handler.Handle(new UpdateCaseStatusCommand(caseId, CaseStatus.Finalizado), default);

        Assert.Equal(CaseStatus.Finalizado.ToString(), response.Status);
    }

[Fact]
    public async Task UpdateStatus_CreadoToFinalizado_InvalidTransition_Throws()
    {
        var (db, caseId, _) = await SeedCaseAsync(CaseStatus.Creado);
        var handler = BuildHandler(db);

        await Assert.ThrowsAsync<InvalidStatusTransitionException>(() =>
            handler.Handle(new UpdateCaseStatusCommand(caseId, CaseStatus.Finalizado), default));
    }

    [Fact]
    public async Task UpdateStatus_FromTerminal_Throws()
    {
        var (db, caseId, _) = await SeedCaseAsync(CaseStatus.Finalizado);
        var handler = BuildHandler(db);

        await Assert.ThrowsAsync<InvalidStatusTransitionException>(() =>
            handler.Handle(new UpdateCaseStatusCommand(caseId, CaseStatus.EnCurso), default));
    }

    [Fact]
    public async Task UpdateStatus_TransitionBlockedByAllowedStatuses_Throws()
    {
        var allowed = new List<CaseStatus> { CaseStatus.Creado, CaseStatus.EnCurso, CaseStatus.Finalizado };
        var (db, caseId, _) = await SeedCaseAsync(CaseStatus.EnCurso, allowed);
        var handler = BuildHandler(db);

        await Assert.ThrowsAsync<InvalidStatusTransitionException>(() =>
            handler.Handle(new UpdateCaseStatusCommand(caseId, CaseStatus.Suspendido), default));
    }

    [Fact]
    public async Task UpdateStatus_TransitionAllowedByDefinition_Succeeds()
    {
        var allowed = new List<CaseStatus> { CaseStatus.Creado, CaseStatus.EnCurso, CaseStatus.Finalizado };
        var (db, caseId, _) = await SeedCaseAsync(CaseStatus.EnCurso, allowed);
        var handler = BuildHandler(db);

        var response = await handler.Handle(new UpdateCaseStatusCommand(caseId, CaseStatus.Finalizado), default);

        Assert.Equal(CaseStatus.Finalizado.ToString(), response.Status);
    }

    [Fact]
    public async Task UpdateStatus_Suspendido_AsOperador_ThrowsUnauthorized()
    {
        var (db, caseId, _) = await SeedCaseAsync(CaseStatus.EnCurso);
        var handler = BuildHandler(db, TestAuthorizationContext.AsOperador());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() =>
            handler.Handle(new UpdateCaseStatusCommand(caseId, CaseStatus.Suspendido), default));
    }

    [Fact]
    public async Task UpdateStatus_Cancelado_AsOperador_ThrowsUnauthorized()
    {
        var (db, caseId, _) = await SeedCaseAsync(CaseStatus.EnCurso);
        var handler = BuildHandler(db, TestAuthorizationContext.AsOperador());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() =>
            handler.Handle(new UpdateCaseStatusCommand(caseId, CaseStatus.Cancelado), default));
    }

    [Fact]
    public async Task UpdateStatus_Suspendido_AsSupervisor_Succeeds()
    {
        var (db, caseId, _) = await SeedCaseAsync(CaseStatus.EnCurso);
        var handler = BuildHandler(db, TestAuthorizationContext.AsSupervisor());

        var response = await handler.Handle(new UpdateCaseStatusCommand(caseId, CaseStatus.Suspendido), default);

        Assert.Equal(CaseStatus.Suspendido.ToString(), response.Status);
    }

    [Fact]
    public async Task UpdateStatus_Suspendido_AsGerente_Succeeds()
    {
        var (db, caseId, _) = await SeedCaseAsync(CaseStatus.EnCurso);
        var handler = BuildHandler(db, TestAuthorizationContext.AsGerente());

        var response = await handler.Handle(new UpdateCaseStatusCommand(caseId, CaseStatus.Suspendido), default);

        Assert.Equal(CaseStatus.Suspendido.ToString(), response.Status);
    }

    [Fact]
    public async Task UpdateStatus_Finalizado_AsOperador_ThrowsUnauthorized()
    {
        var (db, caseId, _) = await SeedCaseAsync(CaseStatus.EnCurso);
        var handler = BuildHandler(db, TestAuthorizationContext.AsOperador());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() =>
            handler.Handle(new UpdateCaseStatusCommand(caseId, CaseStatus.Finalizado), default));
    }

    [Fact]
    public async Task UpdateStatus_Finalizado_AsApi_Succeeds()
    {
        var (db, caseId, _) = await SeedCaseAsync(CaseStatus.EnCurso);
        var handler = BuildHandler(db, new TestAuthorizationContext("Api"));

        var response = await handler.Handle(new UpdateCaseStatusCommand(caseId, CaseStatus.Finalizado), default);

        Assert.Equal(CaseStatus.Finalizado.ToString(), response.Status);
    }

    [Fact]
    public async Task UpdateStatus_Finalizado_AsGerente_Succeeds()
    {
        var (db, caseId, _) = await SeedCaseAsync(CaseStatus.EnCurso);
        var handler = BuildHandler(db, TestAuthorizationContext.AsGerente());

        var response = await handler.Handle(new UpdateCaseStatusCommand(caseId, CaseStatus.Finalizado), default);

        Assert.Equal(CaseStatus.Finalizado.ToString(), response.Status);
    }

    [Fact]
    public async Task UpdateStatus_CaseNotFound_ThrowsValidation()
    {
        using var db = TestDbContext.Create();
        var handler = BuildHandler(db);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new UpdateCaseStatusCommand(Guid.NewGuid(), CaseStatus.EnCurso), default));
    }

    [Fact]
    public async Task UpdateStatus_RegistersTimelineEvent()
    {
        var (db, caseId, _) = await SeedCaseAsync(CaseStatus.Creado);
        var handler = BuildHandler(db);

        await handler.Handle(new UpdateCaseStatusCommand(caseId, CaseStatus.EnCurso), default);

        var events = await db.TimelineEvents.ToListAsync();
        Assert.Single(events);
        Assert.Equal("Inicio de operacion", events[0].Type);
        Assert.Contains("Creado", events[0].Content);
        Assert.Contains("En curso", events[0].Content);
    }

    [Fact]
    public async Task UpdateStatus_GeneratesAuditRecord()
    {
        var (db, caseId, _) = await SeedCaseAsync(CaseStatus.Creado);
        var handler = BuildHandler(db);

        await handler.Handle(new UpdateCaseStatusCommand(caseId, CaseStatus.EnCurso), default);

        var audit = await db.AuditRecords.SingleAsync();
        Assert.Equal(AuditOperation.StatusChange, audit.Operation);
        Assert.Contains("Creado", audit.ChangeJson);
        Assert.Contains("EnCurso", audit.ChangeJson);
    }
}



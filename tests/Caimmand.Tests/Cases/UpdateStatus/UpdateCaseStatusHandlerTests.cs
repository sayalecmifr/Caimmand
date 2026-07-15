using System.Text.Json;
using Caimmand.Application.Cases.UpdateStatus;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using Caimmand.Tests.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Tests.Cases.UpdateStatus;

public class UpdateCaseStatusHandlerTests
{
    private static readonly JsonElement Context =
        JsonDocument.Parse("""{"patientId":1}""").RootElement.Clone();

    private static async Task<(TestDbContext db, Guid caseId)> SeedCaseAsync(CaseStatus status = CaseStatus.Creado)
    {
        var db = TestDbContext.Create();
        db.CaseDefinitions.Add(new CaseDefinition { Code = "X", Name = "X", IsActive = true });
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
        return (db, entity.Id);
    }

    private static UpdateCaseStatusHandler BuildHandler(TestDbContext db) =>
        new(db, new UpdateCaseStatusValidator());

    [Fact]
    public async Task UpdateStatus_CreadoToEnCurso_Succeeds()
    {
        var (db, caseId) = await SeedCaseAsync(CaseStatus.Creado);
        var handler = BuildHandler(db);

        var response = await handler.Handle(new UpdateCaseStatusCommand(caseId, CaseStatus.EnCurso), default);

        Assert.Equal(CaseStatus.EnCurso.ToString(), response.Status);
        var entity = await db.Cases.FirstAsync();
        Assert.Equal(CaseStatus.EnCurso, entity.Status);
    }

    [Fact]
    public async Task UpdateStatus_EnCursoToFinalizado_Succeeds()
    {
        var (db, caseId) = await SeedCaseAsync(CaseStatus.EnCurso);
        var handler = BuildHandler(db);

        var response = await handler.Handle(new UpdateCaseStatusCommand(caseId, CaseStatus.Finalizado), default);

        Assert.Equal(CaseStatus.Finalizado.ToString(), response.Status);
    }

    [Fact]
    public async Task UpdateStatus_CreadoToFinalizado_InvalidTransition_Throws()
    {
        var (db, caseId) = await SeedCaseAsync(CaseStatus.Creado);
        var handler = BuildHandler(db);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new UpdateCaseStatusCommand(caseId, CaseStatus.Finalizado), default));
    }

    [Fact]
    public async Task UpdateStatus_FromTerminal_Throws()
    {
        var (db, caseId) = await SeedCaseAsync(CaseStatus.Finalizado);
        var handler = BuildHandler(db);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new UpdateCaseStatusCommand(caseId, CaseStatus.EnCurso), default));
    }

    [Fact]
    public async Task UpdateStatus_CaseNotFound_Throws()
    {
        using var db = TestDbContext.Create();
        var handler = BuildHandler(db);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new UpdateCaseStatusCommand(Guid.NewGuid(), CaseStatus.EnCurso), default));
    }

    [Fact]
    public async Task UpdateStatus_RegistersTimelineEvent()
    {
        var (db, caseId) = await SeedCaseAsync(CaseStatus.Creado);
        var handler = BuildHandler(db);

        await handler.Handle(new UpdateCaseStatusCommand(caseId, CaseStatus.EnCurso), default);

        var events = await db.TimelineEvents.ToListAsync();
        Assert.Single(events);
        Assert.Equal("Inicio de operacion", events[0].Type);
        Assert.Contains("Creado", events[0].Content);
        Assert.Contains("En curso", events[0].Content);
    }
}
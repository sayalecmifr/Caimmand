using System.Text.Json;
using Caimmand.Application.Audit;
using Caimmand.Application.Tasks.Create;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using Caimmand.Tests.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;
using TaskEntity = Caimmand.Domain.Entities.Task;
using TaskStatus = Caimmand.Domain.Enums.TaskStatus;

namespace Caimmand.Tests.Tasks.Create;

public class CreateTaskHandlerTests
{
    private static readonly JsonElement Context =
        JsonDocument.Parse("""{"patientId":1}""").RootElement.Clone();

    private static async Task<(TestDbContext db, Guid caseId)> SeedCaseAsync()
    {
        var db = TestDbContext.Create();
        db.CaseDefinitions.Add(new CaseDefinition { Code = "X", Name = "X", IsActive = true });
        var entity = new Case
        {
            Id = Guid.NewGuid(),
            Title = "Caso",
            CaseDefinitionCode = "X",
            Status = CaseStatus.Creado,
            SourceSystem = "HIS",
            Context = JsonDocument.Parse("{}"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Cases.Add(entity);
        await db.SaveChangesAsync();
        return (db, entity.Id);
    }

    private static CreateTaskHandler BuildHandler(TestDbContext db) =>
        new(db, new CreateTaskValidator(), new AuditRecorder(db));

    [Fact]
    public async Task Create_PersistsPendiente_AndGeneratesTimelineAndAudit()
    {
        var (db, caseId) = await SeedCaseAsync();
        var handler = BuildHandler(db);

        var response = await handler.Handle(new CreateTaskCommand(caseId, "enviar_sms"), default);

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("Pendiente", response.Status);

        var entity = await db.Tasks.SingleAsync();
        Assert.Equal("enviar_sms", entity.Type);
        Assert.Equal(TaskStatus.Pendiente, entity.Status);

        var timeline = await db.TimelineEvents.SingleAsync();
        Assert.Equal("Tarea creada", timeline.Type);

        var audit = await db.AuditRecords.SingleAsync();
        Assert.Equal(AuditOperation.TaskCreated, audit.Operation);
        Assert.Contains("enviar_sms", audit.ChangeJson);
    }

    [Fact]
    public async Task Create_WithAssignee_GeneratesAsignacionTimelineEvent()
    {
        var (db, caseId) = await SeedCaseAsync();
        var participant = new Participant { Type = ParticipantType.AgenteIA, Reference = "SMS-agente" };
        db.Participants.Add(participant);
        await db.SaveChangesAsync();

        var handler = BuildHandler(db);
        await handler.Handle(new CreateTaskCommand(caseId, "enviar_sms", participant.Id), default);

        var timeline = await db.TimelineEvents.SingleAsync();
        Assert.Equal("Asignacion", timeline.Type);
    }

    [Fact]
    public async Task Create_CaseNotFound_ThrowsValidation()
    {
        using var db = TestDbContext.Create();
        var handler = BuildHandler(db);

        var ex = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            handler.Handle(new CreateTaskCommand(Guid.NewGuid(), "x"), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CreateTaskCommand.CaseId));
    }

    [Fact]
    public async Task Create_UnknownAssignee_ThrowsValidation()
    {
        var (db, caseId) = await SeedCaseAsync();
        var handler = BuildHandler(db);

        var ex = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            handler.Handle(new CreateTaskCommand(caseId, "x", Guid.NewGuid()), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CreateTaskCommand.AssigneeId));
    }

    [Fact]
    public async Task Create_EmptyType_ThrowsValidation()
    {
        var (db, caseId) = await SeedCaseAsync();
        var handler = BuildHandler(db);

        var ex = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            handler.Handle(new CreateTaskCommand(caseId, ""), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CreateTaskCommand.Type));
    }
}
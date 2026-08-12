using System.Text.Json;
using Caimmand.Application.Audit;
using Caimmand.Application.Timeline.AddEvent;
using Caimmand.Domain.Entities;
using Task = System.Threading.Tasks.Task;
using Caimmand.Domain.Enums;
using Caimmand.Tests.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Tests.Timeline.AddEvent;

public class AddTimelineEventHandlerTests
{
    private static readonly JsonElement Context =
        JsonDocument.Parse("""{"patientId":1}""").RootElement.Clone();

    private static async Task<(TestDbContext db, Guid caseId)> SeedCaseAsync()
    {
        var db = TestDbContext.Create();
        db.CaseDefinitions.Add(new CaseDefinition { Code = "APPOINTMENT_REMINDER", Name = "Recordatorio de Turno", IsActive = true });
        var entity = new Case
        {
            Id = Guid.NewGuid(),
            Title = "Caso de prueba",
            CaseDefinitionCode = "APPOINTMENT_REMINDER",
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

    private static AddTimelineEventHandler BuildHandler(TestDbContext db) =>
        new(db, new AddTimelineEventValidator(), new AuditRecorder(db));

    [Fact]
    public async Task AddEvent_FirstEvent_AssignsSequence1()
    {
        var (db, caseId) = await SeedCaseAsync();
        var handler = BuildHandler(db);

        var response = await handler.Handle(
            new AddTimelineEventCommand(caseId, "Creacion", "HIS", "Caso creado."), default);

        Assert.Equal(1, response.Sequence);
        Assert.Equal(1, await db.TimelineEvents.CountAsync());
    }

    [Fact]
    public async Task AddEvent_SecondEvent_AssignsSequence2()
    {
        var (db, caseId) = await SeedCaseAsync();
        var handler = BuildHandler(db);
        await handler.Handle(new AddTimelineEventCommand(caseId, "Aviso", "Agente", "SMS enviado."), default);

        var response = await handler.Handle(
            new AddTimelineEventCommand(caseId, "Recordatorio", "Agente", "Segundo SMS."), default);

        Assert.Equal(2, response.Sequence);
        Assert.Equal(2, await db.TimelineEvents.CountAsync());
    }

    [Fact]
    public async Task AddEvent_CaseNotFound_ThrowsValidation()
    {
        using var db = TestDbContext.Create();
        var handler = BuildHandler(db);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new AddTimelineEventCommand(Guid.NewGuid(), "Aviso", "HIS", "Texto."), default));
    }

    [Fact]
    public async Task AddEvent_EmptyType_ThrowsValidation()
    {
        var (db, caseId) = await SeedCaseAsync();
        var handler = BuildHandler(db);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new AddTimelineEventCommand(caseId, "", "HIS", "Texto."), default));
    }

    [Fact]
    public async Task AddEvent_GeneratesAuditRecord()
    {
        var (db, caseId) = await SeedCaseAsync();
        var handler = BuildHandler(db);

        await handler.Handle(new AddTimelineEventCommand(caseId, "Aviso", "n8n", "SMS enviado."), default);

        var audit = await db.AuditRecords.SingleAsync();
        Assert.Equal(AuditOperation.EventAdded, audit.Operation);
        Assert.Equal("n8n", audit.Origin);
        Assert.Contains("Aviso", audit.ChangeJson);
    }

    [Fact]
    public async Task AddEvent_WithOriginParticipantId_LinksEventToParticipant()
    {
        var (db, caseId) = await SeedCaseAsync();
        var participant = new Participant
        {
            Type = ParticipantType.SistemaExterno,
            Reference = "HIS",
            ExternalId = "HIS-001"
        };
        db.Participants.Add(participant);
        await db.SaveChangesAsync();

        var handler = BuildHandler(db);
        await handler.Handle(
            new AddTimelineEventCommand(caseId, "Aviso", "HIS", "SMS enviado.", participant.Id),
            default);

        var evt = await db.TimelineEvents.SingleAsync();
        Assert.Equal(participant.Id, evt.ParticipantId);
    }

    [Fact]
    public async Task AddEvent_WithUnknownOriginParticipantId_ThrowsValidation()
    {
        var (db, caseId) = await SeedCaseAsync();
        var handler = BuildHandler(db);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new AddTimelineEventCommand(caseId, "Aviso", "HIS", "x.", Guid.NewGuid()), default));
    }
}

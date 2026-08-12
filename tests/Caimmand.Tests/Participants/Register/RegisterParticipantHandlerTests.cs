using System.Text.Json;
using Caimmand.Application.Audit;
using Caimmand.Application.Participants.Register;
using Caimmand.Domain.Entities;
using Task = System.Threading.Tasks.Task;
using Caimmand.Domain.Enums;
using Caimmand.Tests.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Tests.Participants.Register;

public class RegisterParticipantHandlerTests
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

    private static RegisterParticipantHandler BuildHandler(TestDbContext db) =>
        new(db, new RegisterParticipantValidator(), new AuditRecorder(db));

    private static RegisterParticipantCommand Command(
        Guid caseId,
        ParticipantType type = ParticipantType.SistemaExterno,
        string reference = "HIS",
        string? externalId = "HIS-001",
        string rol = "SistemaDeOrigen") =>
        new(caseId, type, reference, externalId, rol);

    [Fact]
    public async Task Register_NewParticipant_CreatesParticipantAndLink()
    {
        var (db, caseId) = await SeedCaseAsync();
        var handler = BuildHandler(db);

        var response = await handler.Handle(Command(caseId), default);

        Assert.NotEqual(Guid.Empty, response.ParticipantId);
        Assert.Equal(caseId, response.CaseId);
        Assert.Equal("SistemaDeOrigen", response.Rol);

        var participant = await db.Participants.SingleAsync();
        Assert.Equal("HIS", participant.Reference);
        Assert.Equal(ParticipantType.SistemaExterno, participant.Type);

        var link = await db.CaseParticipants.SingleAsync();
        Assert.Equal(caseId, link.CaseId);
        Assert.Equal(participant.Id, link.ParticipantId);
        Assert.Equal("SistemaDeOrigen", link.Rol);
    }

    [Fact]
    public async Task Register_GeneratesAuditRecord()
    {
        var (db, caseId) = await SeedCaseAsync();
        var handler = BuildHandler(db);

        await handler.Handle(Command(caseId), default);

        var audit = await db.AuditRecords.SingleAsync();
        Assert.Equal(AuditOperation.ParticipantRegistered, audit.Operation);
        Assert.Equal("HIS", audit.Origin);
        Assert.Contains("HIS-001", audit.ContextRef);
    }

    [Fact]
    public async Task Register_WithExternalId_ReusesExistingParticipant_AcrossCases()
    {
        var (db, firstCaseId) = await SeedCaseAsync();

        var secondCase = new Case
        {
            Id = Guid.NewGuid(),
            Title = "Otro caso",
            CaseDefinitionCode = "X",
            Status = CaseStatus.Creado,
            SourceSystem = "HIS",
            Context = JsonDocument.Parse("{}"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Cases.Add(secondCase);
        await db.SaveChangesAsync();

        var handler = BuildHandler(db);
        var response1 = await handler.Handle(Command(firstCaseId), default);
        var response2 = await handler.Handle(Command(secondCase.Id), default);

        Assert.Equal(response1.ParticipantId, response2.ParticipantId);

        var participantCount = await db.Participants.CountAsync();
        Assert.Equal(1, participantCount);

        var links = await db.CaseParticipants.ToListAsync();
        Assert.Equal(2, links.Count);
        Assert.Contains(links, l => l.CaseId == firstCaseId);
        Assert.Contains(links, l => l.CaseId == secondCase.Id);
    }

    [Fact]
    public async Task Register_AlreadyLinkedSameCase_ThrowsValidation()
    {
        var (db, caseId) = await SeedCaseAsync();
        var handler = BuildHandler(db);
        await handler.Handle(Command(caseId), default);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(Command(caseId), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(RegisterParticipantCommand.Rol));
    }

    [Fact]
    public async Task Register_CaseNotFound_ThrowsValidation()
    {
        using var db = TestDbContext.Create();
        var handler = BuildHandler(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(Command(Guid.NewGuid()), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(RegisterParticipantCommand.CaseId));
    }

    [Fact]
    public async Task Register_EmptyReference_ThrowsValidation()
    {
        var (db, caseId) = await SeedCaseAsync();
        var handler = BuildHandler(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(Command(caseId, reference: ""), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(RegisterParticipantCommand.Reference));
    }
}

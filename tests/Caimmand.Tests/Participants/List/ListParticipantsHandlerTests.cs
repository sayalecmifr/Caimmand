using System.Text.Json;
using Caimmand.Application.Participants.List;
using Caimmand.Domain.Entities;
using Task = System.Threading.Tasks.Task;
using Caimmand.Domain.Enums;
using Caimmand.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Tests.Participants.List;

public class ListParticipantsHandlerTests
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

    private static async Task SeedParticipantAsync(TestDbContext db, Guid caseId, string reference, string rol, ParticipantType type)
    {
        var p = new Participant { Type = type, Reference = reference, ExternalId = reference + "-ext" };
        db.Participants.Add(p);
        await db.SaveChangesAsync();
        db.CaseParticipants.Add(new CaseParticipant { CaseId = caseId, ParticipantId = p.Id, Rol = rol });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task List_ReturnsAllParticipants_ForCase()
    {
        var (db, caseId) = await SeedCaseAsync();
        await SeedParticipantAsync(db, caseId, "Juan Perez", "Paciente", ParticipantType.PersonaExterna);
        await SeedParticipantAsync(db, caseId, "Maria", "Operador", ParticipantType.UsuarioInterno);

        var handler = new ListParticipantsHandler(db);
        var result = await handler.Handle(new ListParticipantsQuery(caseId), default);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Reference == "Juan Perez" && p.Rol == "Paciente");
        Assert.Contains(result, p => p.Reference == "Maria" && p.Rol == "Operador");
    }

    [Fact]
    public async Task List_NoParticipants_ReturnsEmpty()
    {
        var (db, caseId) = await SeedCaseAsync();
        var handler = new ListParticipantsHandler(db);

        var result = await handler.Handle(new ListParticipantsQuery(caseId), default);

        Assert.Empty(result);
    }

    [Fact]
    public async Task List_DoesNotReturnParticipants_FromOtherCases()
    {
        var (db, caseId) = await SeedCaseAsync();
        var otherCaseId = Guid.NewGuid();
        var otherCase = new Case
        {
            Id = otherCaseId,
            Title = "Otro",
            CaseDefinitionCode = "X",
            Status = CaseStatus.Creado,
            SourceSystem = "HIS",
            Context = JsonDocument.Parse("{}"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Cases.Add(otherCase);
        await db.SaveChangesAsync();
        await SeedParticipantAsync(db, otherCaseId, "Externo", "Paciente", ParticipantType.PersonaExterna);

        var handler = new ListParticipantsHandler(db);
        var result = await handler.Handle(new ListParticipantsQuery(caseId), default);

        Assert.Empty(result);
    }
}

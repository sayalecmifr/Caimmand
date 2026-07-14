using System.Text.Json;
using Caimmand.Application.Cases.List;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using Caimmand.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Tests.Cases.List;

public class ListCasesHandlerTests
{
    private static readonly JsonElement Context =
        JsonDocument.Parse("""{"patientId":1}""").RootElement.Clone();

    private static async Task<(TestDbContext db, List<Guid> ids)> SeedAsync(params (CaseStatus status, string code)[] seeds)
    {
        var db = TestDbContext.Create();
        db.CaseDefinitions.Add(new CaseDefinition { Code = "APPOINTMENT_REMINDER", Name = "Recordatorio de Turno", IsActive = true });
        db.CaseDefinitions.Add(new CaseDefinition { Code = "MEDICAL_AUDIT", Name = "Auditoria Medica", IsActive = true });
        var ids = new List<Guid>();
        foreach (var (status, code) in seeds)
        {
            var c = new Case
            {
                Id = Guid.NewGuid(),
                Title = $"Caso {status}",
                Status = status,
                CaseDefinitionCode = code,
                SourceSystem = "HIS",
                Context = JsonDocument.Parse("{}"),
                CreatedAt = DateTime.UtcNow
            };
            db.Cases.Add(c);
            ids.Add(c.Id);
        }
        await db.SaveChangesAsync();
        return (db, ids);
    }

    [Fact]
    public async Task ListCases_NoFilters_ReturnsAllCases()
    {
        var (db, _) = await SeedAsync(
            (CaseStatus.Creado, "APPOINTMENT_REMINDER"),
            (CaseStatus.Finalizado, "MEDICAL_AUDIT"));
        var handler = new ListCasesHandler(db);

        var result = await handler.Handle(new ListCasesQuery(null, null), default);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ListCases_WithStatusFilter_ReturnsOnlyMatching()
    {
        var (db, _) = await SeedAsync(
            (CaseStatus.Creado, "APPOINTMENT_REMINDER"),
            (CaseStatus.Finalizado, "APPOINTMENT_REMINDER"));
        var handler = new ListCasesHandler(db);

        var result = await handler.Handle(new ListCasesQuery(CaseStatus.Finalizado, null), default);

        Assert.Single(result);
        Assert.Equal(CaseStatus.Finalizado, result[0].Status);
    }

    [Fact]
    public async Task ListCases_WithCaseDefinitionFilter_ReturnsOnlyMatching()
    {
        var (db, _) = await SeedAsync(
            (CaseStatus.Creado, "APPOINTMENT_REMINDER"),
            (CaseStatus.Creado, "MEDICAL_AUDIT"));
        var handler = new ListCasesHandler(db);

        var result = await handler.Handle(new ListCasesQuery(null, "MEDICAL_AUDIT"), default);

        Assert.Single(result);
        Assert.Equal("MEDICAL_AUDIT", result[0].CaseDefinitionCode);
        Assert.Equal("Auditoria Medica", result[0].CaseDefinitionName);
    }

    [Fact]
    public async Task ListCases_OrdersByCreatedAtDescending()
    {
        var (db, _) = await SeedAsync(
            (CaseStatus.Creado, "APPOINTMENT_REMINDER"),
            (CaseStatus.Creado, "APPOINTMENT_REMINDER"));
        var firstId = db.Cases.First().Id;
        var earlier = db.Cases.First(c => c.Id != firstId);
        earlier.CreatedAt = DateTime.UtcNow.AddHours(-2);
        await db.SaveChangesAsync();
        var handler = new ListCasesHandler(db);

        var result = await handler.Handle(new ListCasesQuery(null, null), default);

        Assert.Equal(firstId, result[0].Id);
    }

    [Fact]
    public async Task ListCases_EmptyDatabase_ReturnsEmptyList()
    {
        var (db, _) = await SeedAsync();
        var handler = new ListCasesHandler(db);

        var result = await handler.Handle(new ListCasesQuery(null, null), default);

        Assert.Empty(result);
    }
}
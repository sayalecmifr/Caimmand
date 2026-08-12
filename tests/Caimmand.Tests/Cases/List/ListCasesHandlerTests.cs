using System.Text.Json;
using Caimmand.Application.Cases.List;
using Caimmand.Domain.Entities;
using Task = System.Threading.Tasks.Task;
using Caimmand.Domain.Enums;
using Caimmand.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Tests.Cases.List;

public class ListCasesHandlerTests
{
    private static readonly JsonElement Context =
        JsonDocument.Parse("""{"patientId":1}""").RootElement.Clone();

    private static ListCasesHandler BuildHandler(TestDbContext db) =>
        new(db, new InMemoryJsonQueryAdapter());

    private static ListCasesQuery Query(
        CaseStatus? status = null,
        string? code = null,
        string? externalId = null,
        DateTime? createdFrom = null,
        DateTime? createdTo = null,
        DateTime? updatedFrom = null,
        DateTime? updatedTo = null,
        int page = 1,
        int pageSize = 50) =>
        new(status, code, externalId, createdFrom, createdTo, updatedFrom, updatedTo, page, pageSize);

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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
        var handler = BuildHandler(db);

        var result = await handler.Handle(Query(), default);

        Assert.Equal(2, result.Total);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task ListCases_WithStatusFilter_ReturnsOnlyMatching()
    {
        var (db, _) = await SeedAsync(
            (CaseStatus.Creado, "APPOINTMENT_REMINDER"),
            (CaseStatus.Finalizado, "APPOINTMENT_REMINDER"));
        var handler = BuildHandler(db);

        var result = await handler.Handle(Query(status: CaseStatus.Finalizado), default);

        Assert.Single(result.Items);
        Assert.Equal(CaseStatus.Finalizado, result.Items[0].Status);
    }

    [Fact]
    public async Task ListCases_WithCaseDefinitionFilter_ReturnsOnlyMatching()
    {
        var (db, _) = await SeedAsync(
            (CaseStatus.Creado, "APPOINTMENT_REMINDER"),
            (CaseStatus.Creado, "MEDICAL_AUDIT"));
        var handler = BuildHandler(db);

        var result = await handler.Handle(Query(code: "MEDICAL_AUDIT"), default);

        Assert.Single(result.Items);
        Assert.Equal("MEDICAL_AUDIT", result.Items[0].CaseDefinitionCode);
        Assert.Equal("Auditoria Medica", result.Items[0].CaseDefinitionName);
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
        var handler = BuildHandler(db);

        var result = await handler.Handle(Query(), default);

        Assert.Equal(firstId, result.Items[0].Id);
    }

    [Fact]
    public async Task ListCases_EmptyDatabase_ReturnsEmptyList()
    {
        var (db, _) = await SeedAsync();
        var handler = BuildHandler(db);

        var result = await handler.Handle(Query(), default);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
        Assert.Equal(0, result.TotalPages);
    }

    private static async Task<TestDbContext> SeedWithExternalIdAsync(params (string code, string externalId)[] seeds)
    {
        var db = TestDbContext.Create();
        db.CaseDefinitions.Add(new CaseDefinition { Code = "APPOINTMENT_REMINDER", Name = "Recordatorio de Turno", IsActive = true });
        foreach (var (code, externalId) in seeds)
        {
            db.Cases.Add(new Case
            {
                Id = Guid.NewGuid(),
                Title = $"Caso {externalId}",
                Status = CaseStatus.Creado,
                CaseDefinitionCode = code,
                SourceSystem = "HIS",
                Context = JsonDocument.Parse($"{{\"externalId\":\"{externalId}\"}}"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task ListCases_ByExternalId_ReturnsOnlyMatch()
    {
        var db = await SeedWithExternalIdAsync(
            ("APPOINTMENT_REMINDER", "APT-001"),
            ("APPOINTMENT_REMINDER", "APT-002"));
        var handler = BuildHandler(db);

        var result = await handler.Handle(Query(code: "APPOINTMENT_REMINDER", externalId: "APT-001"), default);

        Assert.Single(result.Items);
        Assert.Equal("Caso APT-001", result.Items[0].Title);
    }

    [Fact]
    public async Task ListCases_ByExternalId_NoMatch_ReturnsEmpty()
    {
        var db = await SeedWithExternalIdAsync(
            ("APPOINTMENT_REMINDER", "APT-001"),
            ("APPOINTMENT_REMINDER", "APT-002"));
        var handler = BuildHandler(db);

        var result = await handler.Handle(Query(code: "APPOINTMENT_REMINDER", externalId: "APT-X"), default);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task ListCases_ByExternalId_Null_ReturnsAll()
    {
        var db = await SeedWithExternalIdAsync(
            ("APPOINTMENT_REMINDER", "APT-001"),
            ("APPOINTMENT_REMINDER", "APT-002"));
        var handler = BuildHandler(db);

        var result = await handler.Handle(Query(code: "APPOINTMENT_REMINDER"), default);

        Assert.Equal(2, result.Total);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task ListCases_ByCreatedFrom_ReturnsOnlyAfterDate()
    {
        var (db, _) = await SeedAsync(
            (CaseStatus.Creado, "APPOINTMENT_REMINDER"),
            (CaseStatus.Creado, "APPOINTMENT_REMINDER"));
        var earlier = db.Cases.First();
        earlier.CreatedAt = DateTime.UtcNow.AddDays(-5);
        await db.SaveChangesAsync();
        var handler = BuildHandler(db);

        var result = await handler.Handle(Query(createdFrom: DateTime.UtcNow.AddDays(-1)), default);

        Assert.Single(result.Items);
        Assert.NotEqual(earlier.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task ListCases_ByCreatedTo_ReturnsOnlyBeforeDate()
    {
        var (db, _) = await SeedAsync(
            (CaseStatus.Creado, "APPOINTMENT_REMINDER"),
            (CaseStatus.Creado, "APPOINTMENT_REMINDER"));
        var later = db.Cases.First();
        later.CreatedAt = DateTime.UtcNow.AddDays(5);
        await db.SaveChangesAsync();
        var handler = BuildHandler(db);

        var result = await handler.Handle(Query(createdTo: DateTime.UtcNow.AddDays(1)), default);

        Assert.Single(result.Items);
        Assert.NotEqual(later.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task ListCases_ByCreatedDateRange_ReturnsOnlyWithinRange()
    {
        var (db, _) = await SeedAsync(
            (CaseStatus.Creado, "APPOINTMENT_REMINDER"),
            (CaseStatus.Creado, "APPOINTMENT_REMINDER"),
            (CaseStatus.Creado, "APPOINTMENT_REMINDER"));
        var cases = db.Cases.ToList();
        cases[0].CreatedAt = DateTime.UtcNow.AddDays(-10);
        cases[1].CreatedAt = DateTime.UtcNow.AddDays(-3);
        cases[2].CreatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        var handler = BuildHandler(db);

        var result = await handler.Handle(Query(
            createdFrom: DateTime.UtcNow.AddDays(-5),
            createdTo: DateTime.UtcNow.AddDays(-1)), default);

        Assert.Single(result.Items);
        Assert.Equal(cases[1].Id, result.Items[0].Id);
    }

    [Fact]
    public async Task ListCases_ByUpdatedFrom_ReturnsOnlyAfterDate()
    {
        var (db, _) = await SeedAsync(
            (CaseStatus.Creado, "APPOINTMENT_REMINDER"),
            (CaseStatus.Creado, "APPOINTMENT_REMINDER"));
        var stale = db.Cases.First();
        stale.UpdatedAt = DateTime.UtcNow.AddDays(-10);
        await db.SaveChangesAsync();
        var handler = BuildHandler(db);

        var result = await handler.Handle(Query(updatedFrom: DateTime.UtcNow.AddDays(-1)), default);

        Assert.Single(result.Items);
        Assert.NotEqual(stale.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task ListCases_Pagination_Page1_ReturnsFirstNItems()
    {
        var (db, _) = await SeedAsync();
        for (var i = 0; i < 5; i++)
        {
            db.Cases.Add(new Case
            {
                Id = Guid.NewGuid(),
                Title = $"Caso {i}",
                Status = CaseStatus.Creado,
                CaseDefinitionCode = "APPOINTMENT_REMINDER",
                SourceSystem = "HIS",
                Context = JsonDocument.Parse("{}"),
                CreatedAt = DateTime.UtcNow.AddSeconds(i),
                UpdatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
        var handler = BuildHandler(db);

        var result = await handler.Handle(Query(page: 1, pageSize: 3), default);

        Assert.Equal(5, result.Total);
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(3, result.PageSize);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task ListCases_Pagination_Page2_ReturnsRemainingItems()
    {
        var (db, _) = await SeedAsync();
        for (var i = 0; i < 5; i++)
        {
            db.Cases.Add(new Case
            {
                Id = Guid.NewGuid(),
                Title = $"Caso {i}",
                Status = CaseStatus.Creado,
                CaseDefinitionCode = "APPOINTMENT_REMINDER",
                SourceSystem = "HIS",
                Context = JsonDocument.Parse("{}"),
                CreatedAt = DateTime.UtcNow.AddSeconds(i),
                UpdatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
        var handler = BuildHandler(db);

        var result = await handler.Handle(Query(page: 2, pageSize: 3), default);

        Assert.Equal(5, result.Total);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task ListCases_Pagination_PageOutOfRange_ReturnsEmpty()
    {
        var (db, _) = await SeedAsync(
            (CaseStatus.Creado, "APPOINTMENT_REMINDER"));
        var handler = BuildHandler(db);

        var result = await handler.Handle(Query(page: 10, pageSize: 5), default);

        Assert.Empty(result.Items);
        Assert.Equal(1, result.Total);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task ListCases_Pagination_PreservesFilters()
    {
        var (db, _) = await SeedAsync();
        for (var i = 0; i < 5; i++)
        {
            db.Cases.Add(new Case
            {
                Id = Guid.NewGuid(),
                Title = $"Caso {i}",
                Status = i < 3 ? CaseStatus.Creado : CaseStatus.Finalizado,
                CaseDefinitionCode = "APPOINTMENT_REMINDER",
                SourceSystem = "HIS",
                Context = JsonDocument.Parse("{}"),
                CreatedAt = DateTime.UtcNow.AddSeconds(i),
                UpdatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
        var handler = BuildHandler(db);

        var result = await handler.Handle(Query(status: CaseStatus.Creado, page: 1, pageSize: 2), default);

        Assert.Equal(3, result.Total);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.TotalPages);
        Assert.All(result.Items, item => Assert.Equal(CaseStatus.Creado, item.Status));
    }

    [Fact]
    public async Task ListCases_Pagination_PageSizeOverride()
    {
        var (db, _) = await SeedAsync();
        for (var i = 0; i < 7; i++)
        {
            db.Cases.Add(new Case
            {
                Id = Guid.NewGuid(),
                Title = $"Caso {i}",
                Status = CaseStatus.Creado,
                CaseDefinitionCode = "APPOINTMENT_REMINDER",
                SourceSystem = "HIS",
                Context = JsonDocument.Parse("{}"),
                CreatedAt = DateTime.UtcNow.AddSeconds(i),
                UpdatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
        var handler = BuildHandler(db);

        var result = await handler.Handle(Query(page: 1, pageSize: 100), default);

        Assert.Equal(7, result.Total);
        Assert.Equal(7, result.Items.Count);
        Assert.Equal(1, result.TotalPages);
    }
}
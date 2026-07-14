using System.Text.Json;
using Caimmand.Application.Dashboard.GetDashboardKpis;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using Caimmand.Tests.Infrastructure;

namespace Caimmand.Tests.Dashboard;

public class GetDashboardKpisHandlerTests
{
    private static Case NewCase(CaseStatus status) => new()
    {
        Id = Guid.NewGuid(),
        Title = $"Caso {status}",
        CaseDefinitionCode = "APPOINTMENT_REMINDER",
        Status = status,
        SourceSystem = "HIS",
        Context = JsonDocument.Parse("{}"),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task GetDashboardKpis_EmptyDatabase_ReturnsZeros()
    {
        using var db = TestDbContext.Create();
        var handler = new GetDashboardKpisHandler(db);

        var result = await handler.Handle(new GetDashboardKpisQuery(), default);

        Assert.Equal(0, result.Total);
        Assert.Equal(0, result.Created);
        Assert.Equal(0, result.Finalizados);
        Assert.Equal(0, result.RequierenIntervencion);
    }

    [Fact]
    public async Task GetDashboardKpis_MixedStates_ReturnsCorrectCounts()
    {
        using var db = TestDbContext.Create();
        db.Cases.Add(NewCase(CaseStatus.Creado));
        db.Cases.Add(NewCase(CaseStatus.Creado));
        db.Cases.Add(NewCase(CaseStatus.Suspendido));
        db.Cases.Add(NewCase(CaseStatus.Finalizado));
        await db.SaveChangesAsync();
        var handler = new GetDashboardKpisHandler(db);

        var result = await handler.Handle(new GetDashboardKpisQuery(), default);

        Assert.Equal(4, result.Total);
        Assert.Equal(2, result.Created);
        Assert.Equal(1, result.Finalizados);
        Assert.Equal(1, result.RequierenIntervencion);
    }
}
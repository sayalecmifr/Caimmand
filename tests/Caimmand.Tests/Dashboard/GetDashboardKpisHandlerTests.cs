using System.Text.Json;
using Caimmand.Application.Dashboard.GetDashboardKpis;
using Caimmand.Domain.Entities;
using Task = System.Threading.Tasks.Task;
using TaskEntity = Caimmand.Domain.Entities.Task;
using TaskStatus = Caimmand.Domain.Enums.TaskStatus;
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

    private static TaskEntity NewTask(Guid caseId, TaskStatus status, DateTime? dueAt) => new()
    {
        Id = Guid.NewGuid(),
        CaseId = caseId,
        Type = "enviar_sms",
        Status = status,
        DueAt = dueAt,
        CreatedAt = DateTime.UtcNow
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
        Assert.Equal(0, result.TasksOverdue);
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
        Assert.Equal(0, result.TasksOverdue);
    }

    [Fact]
    public async Task GetDashboardKpis_TasksOverdue_CountsOnlyOpenTasksWithPastDueAt()
    {
        using var db = TestDbContext.Create();
        var caseId = Guid.NewGuid();
        db.Cases.Add(NewCase(CaseStatus.EnCurso));
        var past = DateTime.UtcNow.AddHours(-1);
        var future = DateTime.UtcNow.AddHours(1);

        db.Tasks.Add(NewTask(caseId, TaskStatus.Pendiente, past));
        db.Tasks.Add(NewTask(caseId, TaskStatus.EnProgreso, past));
        db.Tasks.Add(NewTask(caseId, TaskStatus.Pendiente, future));
        db.Tasks.Add(NewTask(caseId, TaskStatus.Completada, past));
        db.Tasks.Add(NewTask(caseId, TaskStatus.Cancelada, past));
        db.Tasks.Add(NewTask(caseId, TaskStatus.Pendiente, null));
        await db.SaveChangesAsync();

        var handler = new GetDashboardKpisHandler(db);
        var result = await handler.Handle(new GetDashboardKpisQuery(), default);

        Assert.Equal(2, result.TasksOverdue);
    }

    [Fact]
    public async Task GetDashboardKpis_TasksOverdue_NoTasks_ReturnsZero()
    {
        using var db = TestDbContext.Create();
        db.Cases.Add(NewCase(CaseStatus.EnCurso));
        await db.SaveChangesAsync();

        var handler = new GetDashboardKpisHandler(db);
        var result = await handler.Handle(new GetDashboardKpisQuery(), default);

        Assert.Equal(0, result.TasksOverdue);
    }
}

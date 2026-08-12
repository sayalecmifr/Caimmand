using System.Text.Json;
using Caimmand.Application.Tasks.List;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using Caimmand.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;
using TaskEntity = Caimmand.Domain.Entities.Task;
using TaskStatus = Caimmand.Domain.Enums.TaskStatus;

namespace Caimmand.Tests.Tasks.List;

public class ListTasksHandlerTests
{
    private static async Task<(TestDbContext db, Guid caseId)> SeedCaseAsync()
    {
        var db = TestDbContext.Create();
        db.CaseDefinitions.Add(new CaseDefinition { Code = "X", Name = "X", IsActive = true });
        var caseEntity = new Case
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
        db.Cases.Add(caseEntity);
        await db.SaveChangesAsync();
        return (db, caseEntity.Id);
    }

    private static async Task SeedTaskAsync(TestDbContext db, Guid caseId, string type, TaskStatus status, Guid? assignee = null)
    {
        db.Tasks.Add(new TaskEntity
        {
            CaseId = caseId,
            Type = type,
            Status = status,
            AssigneeId = assignee,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task List_ReturnsAllTasks_ForCase()
    {
        var (db, caseId) = await SeedCaseAsync();
        await SeedTaskAsync(db, caseId, "t1", TaskStatus.Pendiente);
        await SeedTaskAsync(db, caseId, "t2", TaskStatus.Completada);

        var handler = new ListTasksHandler(db);
        var result = await handler.Handle(new ListTasksQuery(caseId), default);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task List_WithStatusFilter_ReturnsOnlyMatching()
    {
        var (db, caseId) = await SeedCaseAsync();
        await SeedTaskAsync(db, caseId, "t1", TaskStatus.Pendiente);
        await SeedTaskAsync(db, caseId, "t2", TaskStatus.Completada);

        var handler = new ListTasksHandler(db);
        var result = await handler.Handle(new ListTasksQuery(caseId, TaskStatus.Pendiente), default);

        Assert.Single(result);
        Assert.Equal("Pendiente", result[0].Status);
    }

    [Fact]
    public async Task List_WithAssigneeFilter_ReturnsOnlyMatching()
    {
        var (db, caseId) = await SeedCaseAsync();
        var p1 = new Participant { Type = ParticipantType.UsuarioInterno, Reference = "u1" };
        var p2 = new Participant { Type = ParticipantType.UsuarioInterno, Reference = "u2" };
        db.Participants.AddRange(p1, p2);
        await db.SaveChangesAsync();
        await SeedTaskAsync(db, caseId, "t1", TaskStatus.Pendiente, p1.Id);
        await SeedTaskAsync(db, caseId, "t2", TaskStatus.Pendiente, p2.Id);

        var handler = new ListTasksHandler(db);
        var result = await handler.Handle(new ListTasksQuery(caseId, null, p1.Id), default);

        Assert.Single(result);
        Assert.Equal(p1.Id, result[0].AssigneeId);
    }

    [Fact]
    public async Task List_NoTasks_ReturnsEmpty()
    {
        var (db, caseId) = await SeedCaseAsync();
        var handler = new ListTasksHandler(db);

        var result = await handler.Handle(new ListTasksQuery(caseId), default);

        Assert.Empty(result);
    }

    [Fact]
    public async Task List_DoesNotReturnTasks_FromOtherCases()
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
        await SeedTaskAsync(db, otherCaseId, "externa", TaskStatus.Pendiente);

        var handler = new ListTasksHandler(db);
        var result = await handler.Handle(new ListTasksQuery(caseId), default);

        Assert.Empty(result);
    }
}
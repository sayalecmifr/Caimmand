using System.Text.Json;
using Caimmand.Application.Tasks.GetDetail;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using Caimmand.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;
using TaskEntity = Caimmand.Domain.Entities.Task;
using TaskStatus = Caimmand.Domain.Enums.TaskStatus;

namespace Caimmand.Tests.Tasks.GetDetail;

public class GetTaskHandlerTests
{
    [Fact]
    public async Task Get_ExistingTask_ReturnsDetail()
    {
        using var db = TestDbContext.Create();
        db.CaseDefinitions.Add(new CaseDefinition { Code = "X", Name = "X", IsActive = true });
        var caseId = Guid.NewGuid();
        db.Cases.Add(new Case
        {
            Id = caseId,
            Title = "Caso",
            CaseDefinitionCode = "X",
            Status = CaseStatus.Creado,
            SourceSystem = "HIS",
            Context = JsonDocument.Parse("{}"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        var task = new TaskEntity
        {
            CaseId = caseId,
            Type = "enviar_sms",
            Status = TaskStatus.Pendiente,
            DueAt = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        var handler = new GetTaskHandler(db);
        var detail = await handler.Handle(new GetTaskQuery(caseId, task.Id), default);

        Assert.NotNull(detail);
        Assert.Equal(task.Id, detail!.Id);
        Assert.Equal("enviar_sms", detail.Type);
        Assert.Equal("Pendiente", detail.Status);
        Assert.NotNull(detail.DueAt);
    }

    [Fact]
    public async Task Get_UnknownTask_ReturnsNull()
    {
        using var db = TestDbContext.Create();
        db.CaseDefinitions.Add(new CaseDefinition { Code = "X", Name = "X", IsActive = true });
        var caseId = Guid.NewGuid();
        db.Cases.Add(new Case
        {
            Id = caseId,
            Title = "Caso",
            CaseDefinitionCode = "X",
            Status = CaseStatus.Creado,
            SourceSystem = "HIS",
            Context = JsonDocument.Parse("{}"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new GetTaskHandler(db);
        var detail = await handler.Handle(new GetTaskQuery(caseId, Guid.NewGuid()), default);

        Assert.Null(detail);
    }

    [Fact]
    public async Task Get_TaskFromOtherCase_ReturnsNull()
    {
        using var db = TestDbContext.Create();
        db.CaseDefinitions.Add(new CaseDefinition { Code = "X", Name = "X", IsActive = true });
        var caseA = Guid.NewGuid();
        var caseB = Guid.NewGuid();
        db.Cases.Add(new Case
        {
            Id = caseA, Title = "A", CaseDefinitionCode = "X", Status = CaseStatus.Creado, SourceSystem = "HIS",
            Context = JsonDocument.Parse("{}"), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        db.Cases.Add(new Case
        {
            Id = caseB, Title = "B", CaseDefinitionCode = "X", Status = CaseStatus.Creado, SourceSystem = "HIS",
            Context = JsonDocument.Parse("{}"), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        var task = new TaskEntity { CaseId = caseB, Type = "x", Status = TaskStatus.Pendiente, CreatedAt = DateTime.UtcNow };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        var handler = new GetTaskHandler(db);
        var detail = await handler.Handle(new GetTaskQuery(caseA, task.Id), default);

        Assert.Null(detail);
    }
}
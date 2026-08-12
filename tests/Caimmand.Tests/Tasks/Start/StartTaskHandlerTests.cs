using System.Text.Json;
using Caimmand.Application.Audit;
using Caimmand.Application.Authorization;
using Caimmand.Application.Tasks.Start;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using Caimmand.Tests.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;
using TaskEntity = Caimmand.Domain.Entities.Task;
using TaskStatus = Caimmand.Domain.Enums.TaskStatus;

namespace Caimmand.Tests.Tasks.Start;

public class StartTaskHandlerTests
{
    private static readonly JsonElement Context =
        JsonDocument.Parse("""{"patientId":1}""").RootElement.Clone();

    private static async Task<(TestDbContext db, Guid caseId, Guid taskId)> SeedAsync(TaskStatus status)
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
        var task = new TaskEntity
        {
            CaseId = caseEntity.Id,
            Type = "enviar_sms",
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return (db, caseEntity.Id, task.Id);
    }

    private static StartTaskHandler BuildHandler(TestDbContext db, IAuthorizationContext? auth = null) =>
        new(db, new AuditRecorder(db), auth ?? TestAuthorizationContext.AsOperador());

    [Fact]
    public async Task Start_Pendiente_TransitsToEnProgreso_SetsStartedAt_GeneratesAudit()
    {
        var (db, caseId, taskId) = await SeedAsync(TaskStatus.Pendiente);
        var handler = BuildHandler(db);

        var response = await handler.Handle(new StartTaskCommand(caseId, taskId), default);

        Assert.Equal("EnProgreso", response.Status);
        Assert.NotNull(response.StartedAt);

        var entity = await db.Tasks.FirstAsync();
        Assert.Equal(TaskStatus.EnProgreso, entity.Status);
        Assert.NotNull(entity.StartedAt);

        var audit = await db.AuditRecords.SingleAsync();
        Assert.Equal(AuditOperation.TaskStarted, audit.Operation);
    }

    [Fact]
    public async Task Start_AlreadyEnProgreso_ThrowsValidation()
    {
        var (db, caseId, taskId) = await SeedAsync(TaskStatus.EnProgreso);
        var handler = BuildHandler(db);

        var ex = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            handler.Handle(new StartTaskCommand(caseId, taskId), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(StartTaskCommand.TaskId));
    }

    [Fact]
    public async Task Start_Completada_ThrowsValidation()
    {
        var (db, caseId, taskId) = await SeedAsync(TaskStatus.Completada);
        var handler = BuildHandler(db);

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            handler.Handle(new StartTaskCommand(caseId, taskId), default));
    }

[Fact]
    public async Task Start_UnknownTask_ThrowsValidation()
    {
        var (db, caseId, _) = await SeedAsync(TaskStatus.Pendiente);
        var handler = BuildHandler(db);

        var ex = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            handler.Handle(new StartTaskCommand(caseId, Guid.NewGuid()), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(StartTaskCommand.TaskId));
    }

    [Fact]
    public async Task Start_AsApi_Succeeds()
    {
        var (db, caseId, taskId) = await SeedAsync(TaskStatus.Pendiente);
        var handler = BuildHandler(db, TestAuthorizationContext.AsNone());
        var apiAuth = new TestAuthorizationContext("Api");
        handler = new(db, new AuditRecorder(db), apiAuth);

        var response = await handler.Handle(new StartTaskCommand(caseId, taskId), default);

        Assert.Equal("EnProgreso", response.Status);
    }

    [Fact]
    public async Task Start_AsGerente_ThrowsUnauthorized()
    {
        var (db, caseId, taskId) = await SeedAsync(TaskStatus.Pendiente);
        var handler = BuildHandler(db, TestAuthorizationContext.AsGerente());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() =>
            handler.Handle(new StartTaskCommand(caseId, taskId), default));
    }

    [Fact]
    public async Task Start_AsNone_ThrowsUnauthorized()
    {
        var (db, caseId, taskId) = await SeedAsync(TaskStatus.Pendiente);
        var handler = BuildHandler(db, TestAuthorizationContext.AsNone());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() =>
            handler.Handle(new StartTaskCommand(caseId, taskId), default));
    }
}

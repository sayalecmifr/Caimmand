using System.Text.Json;
using Caimmand.Application.Audit;
using Caimmand.Application.Authorization;
using Caimmand.Application.Tasks.Cancel;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using Caimmand.Tests.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;
using TaskEntity = Caimmand.Domain.Entities.Task;
using TaskStatus = Caimmand.Domain.Enums.TaskStatus;

namespace Caimmand.Tests.Tasks.Cancel;

public class CancelTaskHandlerTests
{
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

    private static CancelTaskHandler BuildHandler(TestDbContext db, IAuthorizationContext? auth = null) =>
        new(db, new AuditRecorder(db), auth ?? TestAuthorizationContext.AsOperador());

    [Fact]
    public async Task Cancel_FromPendiente_TransitsToCancelada_SetsCompletedAt()
    {
        var (db, caseId, taskId) = await SeedAsync(TaskStatus.Pendiente);
        var handler = BuildHandler(db);

        var response = await handler.Handle(new CancelTaskCommand(caseId, taskId), default);

        Assert.Equal("Cancelada", response.Status);
        Assert.NotNull(response.CompletedAt);

        var audit = await db.AuditRecords.SingleAsync();
        Assert.Equal(AuditOperation.TaskCancelled, audit.Operation);
    }

    [Fact]
    public async Task Cancel_FromEnProgreso_TransitsToCancelada()
    {
        var (db, caseId, taskId) = await SeedAsync(TaskStatus.EnProgreso);
        var handler = BuildHandler(db);

        var response = await handler.Handle(new CancelTaskCommand(caseId, taskId), default);

        Assert.Equal("Cancelada", response.Status);
    }

    [Fact]
    public async Task Cancel_Completada_ThrowsValidation()
    {
        var (db, caseId, taskId) = await SeedAsync(TaskStatus.Completada);
        var handler = BuildHandler(db);

        var ex = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            handler.Handle(new CancelTaskCommand(caseId, taskId), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CancelTaskCommand.TaskId));
    }

    [Fact]
    public async Task Cancel_AlreadyCancelada_ThrowsValidation()
    {
        var (db, caseId, taskId) = await SeedAsync(TaskStatus.Cancelada);
        var handler = BuildHandler(db);

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            handler.Handle(new CancelTaskCommand(caseId, taskId), default));
    }

[Fact]
    public async Task Cancel_UnknownTask_ThrowsValidation()
    {
        var (db, caseId, _) = await SeedAsync(TaskStatus.Pendiente);
        var handler = BuildHandler(db);

        var ex = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            handler.Handle(new CancelTaskCommand(caseId, Guid.NewGuid()), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CancelTaskCommand.TaskId));
    }

    [Fact]
    public async Task Cancel_AsApi_ThrowsUnauthorized()
    {
        var (db, caseId, taskId) = await SeedAsync(TaskStatus.Pendiente);
        var handler = BuildHandler(db, new TestAuthorizationContext("Api"));

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() =>
            handler.Handle(new CancelTaskCommand(caseId, taskId), default));
    }

    [Fact]
    public async Task Cancel_AsOperador_Succeeds()
    {
        var (db, caseId, taskId) = await SeedAsync(TaskStatus.Pendiente);
        var handler = BuildHandler(db, TestAuthorizationContext.AsOperador());

        var response = await handler.Handle(new CancelTaskCommand(caseId, taskId), default);

        Assert.Equal("Cancelada", response.Status);
    }

    [Fact]
    public async Task Cancel_AsSupervisor_Succeeds()
    {
        var (db, caseId, taskId) = await SeedAsync(TaskStatus.Pendiente);
        var handler = BuildHandler(db, TestAuthorizationContext.AsSupervisor());

        var response = await handler.Handle(new CancelTaskCommand(caseId, taskId), default);

        Assert.Equal("Cancelada", response.Status);
    }
}

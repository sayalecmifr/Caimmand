using System.Text.Json;
using Caimmand.Application.Audit;
using Caimmand.Application.Tasks.Complete;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using Caimmand.Tests.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;
using TaskEntity = Caimmand.Domain.Entities.Task;
using TaskStatus = Caimmand.Domain.Enums.TaskStatus;

namespace Caimmand.Tests.Tasks.Complete;

public class CompleteTaskHandlerTests
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

    private static CompleteTaskHandler BuildHandler(TestDbContext db) => new(db, new AuditRecorder(db), TestAuthorizationContext.AsOperador());

    [Fact]
    public async Task Complete_FromEnProgreso_TransitsToCompletada_SetsResultAndCompletedAt()
    {
        var (db, caseId, taskId) = await SeedAsync(TaskStatus.EnProgreso);
        var handler = BuildHandler(db);

        var response = await handler.Handle(new CompleteTaskCommand(caseId, taskId, "SMS enviado OK"), default);

        Assert.Equal("Completada", response.Status);
        Assert.Equal("SMS enviado OK", response.Result);
        Assert.NotNull(response.CompletedAt);

        var audit = await db.AuditRecords.SingleAsync();
        Assert.Equal(AuditOperation.TaskCompleted, audit.Operation);
        Assert.Contains("SMS enviado OK", audit.ChangeJson);
    }

    [Fact]
    public async Task Complete_FromPendiente_TransitsToCompletada()
    {
        var (db, caseId, taskId) = await SeedAsync(TaskStatus.Pendiente);
        var handler = BuildHandler(db);

        var response = await handler.Handle(new CompleteTaskCommand(caseId, taskId, null), default);

        Assert.Equal("Completada", response.Status);
    }

    [Fact]
    public async Task Complete_AlreadyCompletada_ThrowsValidation()
    {
        var (db, caseId, taskId) = await SeedAsync(TaskStatus.Completada);
        var handler = BuildHandler(db);

        var ex = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            handler.Handle(new CompleteTaskCommand(caseId, taskId, null), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CompleteTaskCommand.TaskId));
    }

    [Fact]
    public async Task Complete_Cancelada_ThrowsValidation()
    {
        var (db, caseId, taskId) = await SeedAsync(TaskStatus.Cancelada);
        var handler = BuildHandler(db);

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            handler.Handle(new CompleteTaskCommand(caseId, taskId, null), default));
    }

    [Fact]
    public async Task Complete_UnknownTask_ThrowsValidation()
    {
        var (db, caseId, _) = await SeedAsync(TaskStatus.EnProgreso);
        var handler = BuildHandler(db);

        var ex = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            handler.Handle(new CompleteTaskCommand(caseId, Guid.NewGuid(), null), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CompleteTaskCommand.TaskId));
    }
}

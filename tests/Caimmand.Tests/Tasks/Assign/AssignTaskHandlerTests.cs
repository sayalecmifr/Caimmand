using System.Text.Json;
using Caimmand.Application.Audit;
using Caimmand.Application.Authorization;
using Caimmand.Application.Tasks.Assign;
using Caimmand.Application.Tasks.Create;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using Caimmand.Tests.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;
using TaskEntity = Caimmand.Domain.Entities.Task;
using TaskStatus = Caimmand.Domain.Enums.TaskStatus;

namespace Caimmand.Tests.Tasks.Assign;

public class AssignTaskHandlerTests
{
    private static readonly JsonElement Context =
        JsonDocument.Parse("""{"patientId":1}""").RootElement.Clone();

    private static async Task<(TestDbContext db, Guid caseId, Guid taskId, Guid participantId)> SeedAsync()
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

        var participant = new Participant { Type = ParticipantType.UsuarioInterno, Reference = "Maria" };
        db.Participants.Add(participant);

        var task = new TaskEntity
        {
            CaseId = caseEntity.Id,
            Type = "enviar_sms",
            Status = TaskStatus.Pendiente,
            CreatedAt = DateTime.UtcNow
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return (db, caseEntity.Id, task.Id, participant.Id);
    }

    private static AssignTaskHandler BuildHandler(TestDbContext db, IAuthorizationContext? auth = null) =>
        new(db, new AuditRecorder(db), auth ?? TestAuthorizationContext.AsSupervisor());

    [Fact]
    public async Task Assign_SetsAssignee_AndGeneratesAudit()
    {
        var (db, caseId, taskId, participantId) = await SeedAsync();
        var handler = BuildHandler(db);

        var response = await handler.Handle(new AssignTaskCommand(caseId, taskId, participantId), default);

        Assert.Equal(participantId, response.AssigneeId);
        var entity = await db.Tasks.FirstAsync();
        Assert.Equal(participantId, entity.AssigneeId);

        var audit = await db.AuditRecords.SingleAsync();
        Assert.Equal(AuditOperation.TaskAssigned, audit.Operation);
    }

    [Fact]
    public async Task Assign_UnknownTask_ThrowsValidation()
    {
        var (db, caseId, _, participantId) = await SeedAsync();
        var handler = BuildHandler(db);

        var ex = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            handler.Handle(new AssignTaskCommand(caseId, Guid.NewGuid(), participantId), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(AssignTaskCommand.TaskId));
    }

[Fact]
    public async Task Assign_UnknownParticipant_ThrowsValidation()
    {
        var (db, caseId, taskId, _) = await SeedAsync();
        var handler = BuildHandler(db);

        var ex = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            handler.Handle(new AssignTaskCommand(caseId, taskId, Guid.NewGuid()), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(AssignTaskCommand.AssigneeId));
    }

    [Fact]
    public async Task Assign_AsOperador_ThrowsUnauthorized()
    {
        var (db, caseId, taskId, participantId) = await SeedAsync();
        var handler = BuildHandler(db, TestAuthorizationContext.AsOperador());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() =>
            handler.Handle(new AssignTaskCommand(caseId, taskId, participantId), default));
    }

    [Fact]
    public async Task Assign_AsApi_ThrowsUnauthorized()
    {
        var (db, caseId, taskId, participantId) = await SeedAsync();
        var handler = BuildHandler(db, new TestAuthorizationContext("Api"));

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() =>
            handler.Handle(new AssignTaskCommand(caseId, taskId, participantId), default));
    }

    [Fact]
    public async Task Assign_AsSupervisor_Succeeds()
    {
        var (db, caseId, taskId, participantId) = await SeedAsync();
        var handler = BuildHandler(db, TestAuthorizationContext.AsSupervisor());

        var response = await handler.Handle(new AssignTaskCommand(caseId, taskId, participantId), default);

        Assert.Equal(participantId, response.AssigneeId);
    }

    [Fact]
    public async Task Assign_AsGerente_Succeeds()
    {
        var (db, caseId, taskId, participantId) = await SeedAsync();
        var handler = BuildHandler(db, TestAuthorizationContext.AsGerente());

        var response = await handler.Handle(new AssignTaskCommand(caseId, taskId, participantId), default);

        Assert.Equal(participantId, response.AssigneeId);
    }
}

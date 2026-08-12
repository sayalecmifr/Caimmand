using System.Text.Json;
using Caimmand.Application.Audit;
using Caimmand.Application.Authorization;
using Caimmand.Domain;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;
using TaskEntity = Caimmand.Domain.Entities.Task;

namespace Caimmand.Application.Tasks.Assign;

public sealed class AssignTaskHandler
{
    private readonly ICaimmandDbContext _db;
    private readonly IAuditRecorder _audit;
    private readonly IAuthorizationContext _authorization;

    public AssignTaskHandler(ICaimmandDbContext db, IAuditRecorder audit, IAuthorizationContext authorization)
    {
        _db = db;
        _audit = audit;
        _authorization = authorization;
    }

    public async Task<AssignTaskResponse> Handle(AssignTaskCommand command, CancellationToken ct)
    {
        if (!_authorization.IsInRole(Roles.Supervisor, Roles.Gerente))
        {
            throw new UnauthorizedOperationException($"{Roles.Supervisor} o {Roles.Gerente}", _authorization.GetCurrentRole() ?? "(ninguno)");
        }
        var entity = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == command.TaskId && t.CaseId == command.CaseId, ct);
        if (entity is null)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(command.TaskId), "La tarea no existe para el caso indicado.")
            });
        }

        var assigneeExists = await _db.Participants.AnyAsync(p => p.Id == command.AssigneeId, ct);
        if (!assigneeExists)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(command.AssigneeId), "El participante asignado no existe.")
            });
        }

        var previousAssignee = entity.AssigneeId;
        entity.AssigneeId = command.AssigneeId;

        var maxSequence = await _db.TimelineEvents
            .Where(e => e.CaseId == entity.CaseId)
            .Select(e => (long?)e.Sequence)
            .MaxAsync(ct) ?? 0;

        _db.TimelineEvents.Add(new TimelineEvent
        {
            CaseId = entity.CaseId,
            Sequence = maxSequence + 1,
            Type = "Asignacion",
            Origin = "Sistema",
            Content = $"Tarea '{entity.Type}' asignada.",
            OccurredAt = DateTime.UtcNow
        });

        var change = JsonSerializer.Serialize(new
        {
            taskId = entity.Id,
            previousAssigneeId = previousAssignee,
            newAssigneeId = command.AssigneeId
        });

        await _audit.RecordAsync(
            entity.CaseId,
            AuditOperation.TaskAssigned,
            "Sistema",
            change,
            contextRef: entity.Id.ToString(),
            ct);

        await _db.SaveChangesAsync(ct);

        return new AssignTaskResponse(entity.Id, entity.CaseId, entity.AssigneeId!.Value, entity.Status.ToString());
    }
}
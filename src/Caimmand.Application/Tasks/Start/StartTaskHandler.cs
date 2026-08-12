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
using TaskStatus = Caimmand.Domain.Enums.TaskStatus;

namespace Caimmand.Application.Tasks.Start;

public sealed class StartTaskHandler
{
    private readonly ICaimmandDbContext _db;
    private readonly IAuditRecorder _audit;
    private readonly IAuthorizationContext _authorization;

    public StartTaskHandler(ICaimmandDbContext db, IAuditRecorder audit, IAuthorizationContext authorization)
    {
        _db = db;
        _audit = audit;
        _authorization = authorization;
    }

    public async Task<StartTaskResponse> Handle(StartTaskCommand command, CancellationToken ct)
    {
        if (!_authorization.IsInRole(Roles.Operador, Roles.Supervisor, Roles.Api))
        {
            throw new UnauthorizedOperationException($"{Roles.Operador}, {Roles.Supervisor} o sistema externo ({Roles.Api})", _authorization.GetCurrentRole() ?? "(ninguno)");
        }
        var entity = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == command.TaskId && t.CaseId == command.CaseId, ct);
        if (entity is null)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(command.TaskId), "La tarea no existe para el caso indicado.")
            });
        }

        if (entity.Status != TaskStatus.Pendiente)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(command.TaskId),
                    $"Solo se puede iniciar una tarea Pendiente. Estado actual: {entity.Status}.")
            });
        }

        var previousStatus = entity.Status;
        var now = DateTime.UtcNow;
        entity.Status = TaskStatus.EnProgreso;
        entity.StartedAt = now;

        var maxSequence = await _db.TimelineEvents
            .Where(e => e.CaseId == entity.CaseId)
            .Select(e => (long?)e.Sequence)
            .MaxAsync(ct) ?? 0;

        _db.TimelineEvents.Add(new TimelineEvent
        {
            CaseId = entity.CaseId,
            Sequence = maxSequence + 1,
            Type = "Tarea iniciada",
            Origin = "Operador",
            Content = $"Tarea '{entity.Type}' iniciada.",
            OccurredAt = now
        });

        var change = JsonSerializer.Serialize(new
        {
            taskId = entity.Id,
            from = previousStatus.ToString(),
            to = TaskStatus.EnProgreso.ToString()
        });

        await _audit.RecordAsync(
            entity.CaseId,
            AuditOperation.TaskStarted,
            "Operador",
            change,
            contextRef: entity.Id.ToString(),
            ct);

        await _db.SaveChangesAsync(ct);

        return new StartTaskResponse(entity.Id, entity.CaseId, entity.Status.ToString(), entity.StartedAt);
    }
}
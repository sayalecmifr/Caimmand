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

namespace Caimmand.Application.Tasks.Cancel;

public sealed class CancelTaskHandler
{
    private readonly ICaimmandDbContext _db;
    private readonly IAuditRecorder _audit;
    private readonly IAuthorizationContext _authorization;

    public CancelTaskHandler(ICaimmandDbContext db, IAuditRecorder audit, IAuthorizationContext authorization)
    {
        _db = db;
        _audit = audit;
        _authorization = authorization;
    }

    public async Task<CancelTaskResponse> Handle(CancelTaskCommand command, CancellationToken ct)
    {
        if (!_authorization.IsInRole(Roles.Operador, Roles.Supervisor))
        {
            throw new UnauthorizedOperationException($"{Roles.Operador} o {Roles.Supervisor}", _authorization.GetCurrentRole() ?? "(ninguno)");
        }
        var entity = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == command.TaskId && t.CaseId == command.CaseId, ct);
        if (entity is null)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(command.TaskId), "La tarea no existe para el caso indicado.")
            });
        }

        if (entity.Status == TaskStatus.Completada || entity.Status == TaskStatus.Cancelada)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(command.TaskId),
                    $"No se puede cancelar una tarea en estado terminal ({entity.Status}).")
            });
        }

        var previousStatus = entity.Status;
        var now = DateTime.UtcNow;
        entity.Status = TaskStatus.Cancelada;
        entity.CompletedAt = now;

        var maxSequence = await _db.TimelineEvents
            .Where(e => e.CaseId == entity.CaseId)
            .Select(e => (long?)e.Sequence)
            .MaxAsync(ct) ?? 0;

        _db.TimelineEvents.Add(new TimelineEvent
        {
            CaseId = entity.CaseId,
            Sequence = maxSequence + 1,
            Type = "Tarea cancelada",
            Origin = "Operador",
            Content = $"Tarea '{entity.Type}' cancelada.",
            OccurredAt = now
        });

        var change = JsonSerializer.Serialize(new
        {
            taskId = entity.Id,
            from = previousStatus.ToString(),
            to = TaskStatus.Cancelada.ToString()
        });

        await _audit.RecordAsync(
            entity.CaseId,
            AuditOperation.TaskCancelled,
            "Operador",
            change,
            contextRef: entity.Id.ToString(),
            ct);

        await _db.SaveChangesAsync(ct);

        return new CancelTaskResponse(entity.Id, entity.CaseId, entity.Status.ToString(), entity.CompletedAt);
    }
}
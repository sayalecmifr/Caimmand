using System.Text.Json;
using Caimmand.Application.Audit;
using Caimmand.Domain;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;
using TaskEntity = Caimmand.Domain.Entities.Task;
using TaskStatus = Caimmand.Domain.Enums.TaskStatus;

namespace Caimmand.Application.Tasks.Create;

public sealed class CreateTaskHandler
{
    private readonly ICaimmandDbContext _db;
    private readonly IValidator<CreateTaskCommand> _validator;
    private readonly IAuditRecorder _audit;

    public CreateTaskHandler(ICaimmandDbContext db, IValidator<CreateTaskCommand> validator, IAuditRecorder audit)
    {
        _db = db;
        _validator = validator;
        _audit = audit;
    }

    public async Task<CreateTaskResponse> Handle(CreateTaskCommand command, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var caseExists = await _db.Cases.AnyAsync(c => c.Id == command.CaseId, ct);
        if (!caseExists)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(command.CaseId), "El caso no existe.")
            });
        }

        if (command.AssigneeId is not null)
        {
            var assigneeExists = await _db.Participants.AnyAsync(p => p.Id == command.AssigneeId.Value, ct);
            if (!assigneeExists)
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure(nameof(command.AssigneeId), "El participante asignado no existe.")
                });
            }
        }

        var now = DateTime.UtcNow;
        var entity = new TaskEntity
        {
            CaseId = command.CaseId,
            Type = command.Type,
            AssigneeId = command.AssigneeId,
            Status = TaskStatus.Pendiente,
            CreatedAt = now,
            DueAt = command.DueAt
        };

        _db.Tasks.Add(entity);

        var maxSequence = await _db.TimelineEvents
            .Where(e => e.CaseId == command.CaseId)
            .Select(e => (long?)e.Sequence)
            .MaxAsync(ct) ?? 0;

        _db.TimelineEvents.Add(new TimelineEvent
        {
            CaseId = command.CaseId,
            Sequence = maxSequence + 1,
            Type = command.AssigneeId is null ? "Tarea creada" : "Asignacion",
            Origin = "Sistema",
            Content = command.AssigneeId is null
                ? $"Tarea '{command.Type}' creada."
                : $"Tarea '{command.Type}' creada y asignada.",
            OccurredAt = now
        });

        var change = JsonSerializer.Serialize(new
        {
            taskId = entity.Id,
            type = command.Type,
            assigneeId = command.AssigneeId,
            dueAt = command.DueAt,
            status = TaskStatus.Pendiente.ToString()
        });

        await _audit.RecordAsync(
            command.CaseId,
            AuditOperation.TaskCreated,
            "Sistema",
            change,
            contextRef: entity.Id.ToString(),
            ct);

        await _db.SaveChangesAsync(ct);

        return new CreateTaskResponse(
            entity.Id,
            entity.CaseId,
            entity.Type,
            entity.Status.ToString(),
            entity.AssigneeId,
            entity.CreatedAt);
    }
}
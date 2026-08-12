using System.Text.Json;
using Caimmand.Application.Audit;
using Caimmand.Application.Authorization;
using Caimmand.Domain;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using Caimmand.Domain.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace Caimmand.Application.Cases.UpdateStatus;

public sealed class UpdateCaseStatusHandler
{
    private readonly ICaimmandDbContext _db;
    private readonly IValidator<UpdateCaseStatusCommand> _validator;
    private readonly IAuditRecorder _audit;
    private readonly IAuthorizationContext _authorization;

    public UpdateCaseStatusHandler(
        ICaimmandDbContext db,
        IValidator<UpdateCaseStatusCommand> validator,
        IAuditRecorder audit,
        IAuthorizationContext authorization)
    {
        _db = db;
        _validator = validator;
        _audit = audit;
        _authorization = authorization;
    }

    public async Task<UpdateCaseStatusResponse> Handle(UpdateCaseStatusCommand command, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var entity = await _db.Cases.FirstOrDefaultAsync(c => c.Id == command.Id, ct);
        if (entity is null)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(command.Id), "El caso no existe.")
            });
        }

        var definition = await _db.CaseDefinitions
            .FirstOrDefaultAsync(d => d.Code == entity.CaseDefinitionCode, ct);

        var allowedStatuses = definition?.AllowedStatuses;

        if (!CaseStatusTransitions.IsValid(entity.Status, command.NewStatus, allowedStatuses))
        {
            throw new InvalidStatusTransitionException(entity.Status, command.NewStatus);
        }

        RequireRoleForTransition(command.NewStatus);

        var oldStatus = entity.Status;
        entity.Status = command.NewStatus;
        entity.UpdatedAt = DateTime.UtcNow;

        var maxSequence = await _db.TimelineEvents
            .Where(e => e.CaseId == entity.Id)
            .Select(e => (long?)e.Sequence)
            .MaxAsync(ct) ?? 0;

        var actingAs = _authorization.GetCurrentRole() ?? "Operador";

        _db.TimelineEvents.Add(new TimelineEvent
        {
            CaseId = entity.Id,
            Sequence = maxSequence + 1,
            Type = GetTransitionType(oldStatus, command.NewStatus),
            Origin = actingAs,
            Content = $"Estado cambiado de {StatusLabel(oldStatus)} a {StatusLabel(command.NewStatus)}.",
            OccurredAt = DateTime.UtcNow
        });

        var change = JsonSerializer.Serialize(new
        {
            from = oldStatus.ToString(),
            to = command.NewStatus.ToString()
        });

        await _audit.RecordAsync(
            entity.Id,
            AuditOperation.StatusChange,
            actingAs,
            change,
            contextRef: null,
            ct);

        await _db.SaveChangesAsync(ct);

        return new UpdateCaseStatusResponse(entity.Id, entity.Status.ToString(), entity.UpdatedAt);
    }

    private void RequireRoleForTransition(CaseStatus newStatus)
    {
        if (newStatus == CaseStatus.Suspendido || newStatus == CaseStatus.Cancelado)
        {
            if (!_authorization.IsInRole(Roles.Supervisor, Roles.Gerente))
            {
                throw new UnauthorizedOperationException(
                    $"{Roles.Supervisor} o {Roles.Gerente}",
                    _authorization.GetCurrentRole() ?? "(ninguno)");
            }
        }
        else if (newStatus == CaseStatus.Finalizado)
        {
            if (!_authorization.IsInRole(Roles.Supervisor, Roles.Gerente, Roles.Api))
            {
                throw new UnauthorizedOperationException(
                    $"{Roles.Supervisor}, {Roles.Gerente} o sistema externo ({Roles.Api})",
                    _authorization.GetCurrentRole() ?? "(ninguno)");
            }
        }
    }

    private static string GetTransitionType(CaseStatus from, CaseStatus to) => (from, to) switch
    {
        (CaseStatus.Creado, CaseStatus.EnCurso) => "Inicio de operacion",
        (CaseStatus.EnCurso, CaseStatus.Suspendido) => "Suspension",
        (CaseStatus.Suspendido, CaseStatus.EnCurso) => "Reactivacion",
        (CaseStatus.EnCurso, CaseStatus.Finalizado) => "Finalizacion",
        (CaseStatus.EnCurso, CaseStatus.Cancelado) => "Cancelacion",
        (CaseStatus.Suspendido, CaseStatus.Cancelado) => "Cancelacion",
        _ => "Cambio de estado"
    };

    private static string StatusLabel(CaseStatus s) => s switch
    {
        CaseStatus.Creado => "Creado",
        CaseStatus.EnCurso => "En curso",
        CaseStatus.Suspendido => "Suspendido",
        CaseStatus.Finalizado => "Finalizado",
        CaseStatus.Cancelado => "Cancelado",
        _ => s.ToString()
    };
}
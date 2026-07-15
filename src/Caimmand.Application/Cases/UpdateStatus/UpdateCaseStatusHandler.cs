using Caimmand.Domain;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Application.Cases.UpdateStatus;

public sealed class UpdateCaseStatusHandler
{
    private readonly ICaimmandDbContext _db;
    private readonly IValidator<UpdateCaseStatusCommand> _validator;

    public UpdateCaseStatusHandler(ICaimmandDbContext db, IValidator<UpdateCaseStatusCommand> validator)
    {
        _db = db;
        _validator = validator;
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

        if (!CaseStatusTransitions.IsValid(entity.Status, command.NewStatus))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(command.NewStatus),
                    $"Transicion no valida: {entity.Status} -> {command.NewStatus}.")
            });
        }

        var oldStatus = entity.Status;
        entity.Status = command.NewStatus;
        entity.UpdatedAt = DateTime.UtcNow;

        var maxSequence = await _db.TimelineEvents
            .Where(e => e.CaseId == entity.Id)
            .Select(e => (long?)e.Sequence)
            .MaxAsync(ct) ?? 0;

        _db.TimelineEvents.Add(new TimelineEvent
        {
            CaseId = entity.Id,
            Sequence = maxSequence + 1,
            Type = GetTransitionType(oldStatus, command.NewStatus),
            Origin = "Operador",
            Content = $"Estado cambiado de {StatusLabel(oldStatus)} a {StatusLabel(command.NewStatus)}.",
            OccurredAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        return new UpdateCaseStatusResponse(entity.Id, entity.Status.ToString(), entity.UpdatedAt);
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
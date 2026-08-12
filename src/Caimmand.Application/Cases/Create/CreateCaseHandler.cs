using System.Text.Json;
using Caimmand.Application.Audit;
using Caimmand.Domain;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace Caimmand.Application.Cases.Create;

public sealed class CreateCaseHandler
{
    private readonly ICaimmandDbContext _db;
    private readonly IValidator<CreateCaseCommand> _validator;
    private readonly IAuditRecorder _audit;

    public CreateCaseHandler(ICaimmandDbContext db, IValidator<CreateCaseCommand> validator, IAuditRecorder audit)
    {
        _db = db;
        _validator = validator;
        _audit = audit;
    }

    public async Task<CreateCaseResponse> Handle(CreateCaseCommand command, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var definition = await _db.CaseDefinitions
            .FirstOrDefaultAsync(d => d.Code == command.CaseDefinitionCode, ct);

        if (definition is null)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(command.CaseDefinitionCode), "CaseDefinition inexistente.")
            });
        }

        if (!definition.IsActive)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(command.CaseDefinitionCode), "CaseDefinition inactiva.")
            });
        }

        var now = DateTime.UtcNow;
        var entity = new Case
        {
            CaseDefinitionCode = command.CaseDefinitionCode,
            Title = command.Title,
            Status = CaseStatus.Creado,
            Context = JsonDocument.Parse(command.Context.GetRawText()),
            SourceSystem = command.SourceSystem,
            Priority = command.Priority ?? definition.DefaultPriority,
            Sla = command.Sla ?? definition.DefaultSla,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Cases.Add(entity);
        _db.TimelineEvents.Add(new TimelineEvent
        {
            CaseId = entity.Id,
            Sequence = 1,
            Type = "Creacion",
            Origin = command.SourceSystem,
            Content = $"Caso creado por {command.SourceSystem}.",
            OccurredAt = now
        });

        var change = JsonSerializer.Serialize(new
        {
            caseDefinitionCode = entity.CaseDefinitionCode,
            title = entity.Title,
            sourceSystem = entity.SourceSystem,
            priority = entity.Priority,
            sla = entity.Sla?.ToString(),
            status = entity.Status.ToString()
        });

        await _audit.RecordAsync(
            entity.Id,
            AuditOperation.CaseCreation,
            command.SourceSystem,
            change,
            contextRef: null,
            ct);

        await _db.SaveChangesAsync(ct);

        return new CreateCaseResponse(entity.Id, entity.Status.ToString(), entity.CreatedAt);
    }
}
using System.Text.Json;
using Caimmand.Domain;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Application.Cases.Create;

public sealed class CreateCaseHandler
{
    private readonly ICaimmandDbContext _db;
    private readonly IValidator<CreateCaseCommand> _validator;

    public CreateCaseHandler(ICaimmandDbContext db, IValidator<CreateCaseCommand> validator)
    {
        _db = db;
        _validator = validator;
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

        await _db.SaveChangesAsync(ct);

        return new CreateCaseResponse(entity.Id, entity.Status.ToString(), entity.CreatedAt);
    }
}
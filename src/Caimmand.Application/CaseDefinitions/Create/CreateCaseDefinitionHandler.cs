using Caimmand.Domain;
using Caimmand.Domain.Entities;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Application.CaseDefinitions.Create;

public sealed class CreateCaseDefinitionHandler
{
    private readonly ICaimmandDbContext _db;
    private readonly IValidator<CreateCaseDefinitionCommand> _validator;

    public CreateCaseDefinitionHandler(ICaimmandDbContext db, IValidator<CreateCaseDefinitionCommand> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<CreateCaseDefinitionResponse> Handle(CreateCaseDefinitionCommand command, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var existing = await _db.CaseDefinitions
            .AnyAsync(d => d.Code == command.Code, ct);
        if (existing)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(command.Code), "Ya existe una CaseDefinition con ese Code.")
            });
        }

        var entity = new CaseDefinition
        {
            Code = command.Code,
            Name = command.Name,
            Description = command.Description,
            Category = command.Category,
            IsActive = true,
            DefaultPriority = command.DefaultPriority,
            DisplayColor = command.DisplayColor,
            DisplayIcon = command.DisplayIcon
        };

        _db.CaseDefinitions.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new CreateCaseDefinitionResponse(entity.Id, entity.Code);
    }
}
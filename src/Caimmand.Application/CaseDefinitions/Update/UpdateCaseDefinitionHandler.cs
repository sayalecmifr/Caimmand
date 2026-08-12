using Caimmand.Application.Authorization;
using Caimmand.Domain;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace Caimmand.Application.CaseDefinitions.Update;

public sealed class UpdateCaseDefinitionHandler
{
    private readonly ICaimmandDbContext _db;
    private readonly IValidator<UpdateCaseDefinitionCommand> _validator;
    private readonly IAuthorizationContext _authorization;

    public UpdateCaseDefinitionHandler(
        ICaimmandDbContext db,
        IValidator<UpdateCaseDefinitionCommand> validator,
        IAuthorizationContext authorization)
    {
        _db = db;
        _validator = validator;
        _authorization = authorization;
    }

    public async Task<UpdateCaseDefinitionResponse> Handle(UpdateCaseDefinitionCommand command, CancellationToken ct)
    {
        if (!_authorization.IsInRole(Roles.Gerente))
        {
            throw new UnauthorizedOperationException(Roles.Gerente, _authorization.GetCurrentRole() ?? "(ninguno)");
        }

        var validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var entity = await _db.CaseDefinitions.FirstOrDefaultAsync(d => d.Id == command.Id, ct);
        if (entity is null)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(command.Id), "La CaseDefinition no existe.")
            });
        }

        entity.Name = command.Name;
        entity.Description = command.Description;
        entity.Category = command.Category;
        entity.DefaultPriority = command.DefaultPriority;
        entity.DisplayColor = command.DisplayColor;
        entity.DisplayIcon = command.DisplayIcon;
        if (command.AllowedStatuses is not null)
        {
            entity.AllowedStatuses = command.AllowedStatuses;
        }

        await _db.SaveChangesAsync(ct);

        return new UpdateCaseDefinitionResponse(entity.Id, entity.Code, entity.IsActive);
    }
}
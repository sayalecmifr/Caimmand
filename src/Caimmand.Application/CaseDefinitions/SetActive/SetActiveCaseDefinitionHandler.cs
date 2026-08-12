using Caimmand.Application.Authorization;
using Caimmand.Domain;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace Caimmand.Application.CaseDefinitions.SetActive;

public sealed class SetActiveCaseDefinitionHandler
{
    private readonly ICaimmandDbContext _db;
    private readonly IAuthorizationContext _authorization;

    public SetActiveCaseDefinitionHandler(ICaimmandDbContext db, IAuthorizationContext authorization)
    {
        _db = db;
        _authorization = authorization;
    }

    public async Task<SetActiveCaseDefinitionResponse> Handle(SetActiveCaseDefinitionCommand command, CancellationToken ct)
    {
        if (!_authorization.IsInRole(Roles.Gerente))
        {
            throw new UnauthorizedOperationException(Roles.Gerente, _authorization.GetCurrentRole() ?? "(ninguno)");
        }

        var entity = await _db.CaseDefinitions.FirstOrDefaultAsync(d => d.Id == command.Id, ct);
        if (entity is null)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(command.Id), "La CaseDefinition no existe.")
            });
        }

        entity.IsActive = command.IsActive;
        await _db.SaveChangesAsync(ct);

        return new SetActiveCaseDefinitionResponse(entity.Id, entity.IsActive);
    }
}
namespace Caimmand.Application.Authorization;

public sealed class UnauthorizedOperationException : Exception
{
    public string RequiredRoles { get; }
    public string ActualRole { get; }

    public UnauthorizedOperationException(string requiredRoles, string actualRole)
        : base($"Operacion no autorizada. Requiere rol: {requiredRoles}. Rol actual: {actualRole}.")
    {
        RequiredRoles = requiredRoles;
        ActualRole = actualRole;
    }
}
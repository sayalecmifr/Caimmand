namespace Caimmand.Application.Authorization;

public interface IAuthorizationContext
{
    string? GetCurrentRole();
    bool IsInRole(params string[] roles);
}
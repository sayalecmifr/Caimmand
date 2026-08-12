using Caimmand.Application.Authorization;

namespace Caimmand.Tests.Infrastructure;

internal sealed class TestAuthorizationContext : IAuthorizationContext
{
    private readonly string? _role;

    public TestAuthorizationContext(string? role = "Gerente")
    {
        _role = role;
    }

    public static TestAuthorizationContext AsGerente() => new(Roles.Gerente);
    public static TestAuthorizationContext AsSupervisor() => new(Roles.Supervisor);
    public static TestAuthorizationContext AsOperador() => new(Roles.Operador);
    public static TestAuthorizationContext AsNone() => new(null);

    public string? GetCurrentRole() => _role;

    public bool IsInRole(params string[] roles) =>
        !string.IsNullOrWhiteSpace(_role) && roles.Contains(_role);
}
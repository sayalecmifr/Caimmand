using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Caimmand.Web.Authorization;

using Caimmand.Application.Authorization;

public sealed class HttpAuthorizationContext : IAuthorizationContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpAuthorizationContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetCurrentRole()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user is null)
        {
            return null;
        }

        return user.FindFirst(ClaimTypes.Role)?.Value
            ?? user.FindFirst("Role")?.Value;
    }

    public bool IsInRole(params string[] roles)
    {
        var current = GetCurrentRole();
        if (string.IsNullOrWhiteSpace(current))
        {
            return false;
        }

        return roles.Contains(current);
    }
}
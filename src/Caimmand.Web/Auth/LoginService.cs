using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Caimmand.Web.Auth;

public sealed class LoginService
{
    public const string ApiKeyHeaderValue = "X-API-Key";

    private readonly AuthOptions _options;

    public LoginService(AuthOptions options)
    {
        _options = options;
    }

    public bool TryValidate(string username, string password, out AuthUser? user)
    {
        user = _options.Users.FirstOrDefault(u =>
            string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase)
            && u.Password == password);
        return user is not null;
    }

    public bool IsValidApiKey(string? apiKey)
    {
        return !string.IsNullOrWhiteSpace(apiKey)
            && string.Equals(apiKey, _options.ApiKey, StringComparison.Ordinal);
    }

    public ClaimsPrincipal BuildPrincipal(AuthUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    public ClaimsPrincipal BuildApiKeyPrincipal()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "api"),
            new(ClaimTypes.Role, "Api")
        };
        var identity = new ClaimsIdentity(claims, "ApiKey");
        return new ClaimsPrincipal(identity);
    }
}
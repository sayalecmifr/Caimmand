namespace Caimmand.Web.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string ApiKey { get; set; } = string.Empty;
    public string CookieName { get; set; } = "Caimmand.Auth";
    public List<AuthUser> Users { get; set; } = [];
}

public sealed class AuthUser
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
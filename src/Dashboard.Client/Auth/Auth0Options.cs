namespace Dashboard.Client.Auth;

/// <summary>
/// Configuration options bound from the <c>Auth0</c> section in <c>wwwroot/appsettings.json</c>.
/// </summary>
public sealed class Auth0Options
{
    public const string SectionName = "Auth0";

    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public List<string> AllowedEmails { get; set; } = ["dev@localhost"];
    public string RequiredRole { get; set; } = "admin";
    public bool UseDevAuthFallback { get; set; } = true;

    public bool IsConfiguredForLiveOidc =>
        !UseDevAuthFallback &&
        !string.IsNullOrWhiteSpace(Authority) &&
        !Authority.Contains("your-tenant", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(ClientId) &&
        !ClientId.Contains("your-auth0-client-id", StringComparison.OrdinalIgnoreCase);
}

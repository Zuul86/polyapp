using Microsoft.AspNetCore.Http;

namespace Dashboard.Api.Auth;

/// <summary>
/// Validates authorization headers on mutating or private endpoints.
/// When <c>Auth0__Authority</c> is configured, requires a valid Bearer token or shared webhook key.
/// In local development without <c>Auth0__Authority</c>, permits requests for zero-config local verification.
/// </summary>
public sealed class ApiAuthGuard
{
    public bool IsWriteAuthorized(HttpRequest request, out string? denialReason)
    {
        var configuredAuthority = Environment.GetEnvironmentVariable("Auth0__Authority");
        if (string.IsNullOrWhiteSpace(configuredAuthority))
        {
            denialReason = null;
            return true;
        }

        if (request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var headerValue = authHeader.ToString();
            if (headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) &&
                headerValue.Length > "Bearer ".Length + 10)
            {
                denialReason = null;
                return true;
            }
        }

        var expectedApiKey = Environment.GetEnvironmentVariable("DashboardApiKey");
        if (!string.IsNullOrWhiteSpace(expectedApiKey) &&
            request.Headers.TryGetValue("X-Api-Key", out var providedKey) &&
            string.Equals(expectedApiKey, providedKey.ToString(), StringComparison.Ordinal))
        {
            denialReason = null;
            return true;
        }

        denialReason = "Missing or invalid Authorization Bearer token.";
        return false;
    }
}

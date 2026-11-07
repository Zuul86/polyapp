using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Dashboard.Client.Auth;

/// <summary>
/// Local development <see cref="AuthenticationStateProvider"/> used when <see cref="Auth0Options.UseDevAuthFallback"/> is enabled,
/// allowing interactive verification of Unauthenticated, Authorized Admin, and Unauthorized Guest states without external OIDC setup.
/// </summary>
public sealed class DevAuthStateProvider : AuthenticationStateProvider
{
    public enum DevIdentityMode
    {
        Unauthenticated,
        AuthorizedOwner,
        UnauthorizedGuest
    }

    private DevIdentityMode _currentMode = DevIdentityMode.Unauthenticated;

    public DevIdentityMode CurrentMode => _currentMode;

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var principal = _currentMode switch
        {
            DevIdentityMode.AuthorizedOwner => new ClaimsPrincipal(
                new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, "Local Owner"),
                    new Claim(ClaimTypes.Email, "dev@localhost"),
                    new Claim("email", "dev@localhost"),
                    new Claim(ClaimTypes.Role, "admin"),
                    new Claim("roles", "admin")
                ], authenticationType: "Auth0-DevFallback")),

            DevIdentityMode.UnauthorizedGuest => new ClaimsPrincipal(
                new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, "Guest User"),
                    new Claim(ClaimTypes.Email, "guest@example.com"),
                    new Claim("email", "guest@example.com"),
                    new Claim(ClaimTypes.Role, "viewer")
                ], authenticationType: "Auth0-DevFallback")),

            _ => new ClaimsPrincipal(new ClaimsIdentity())
        };

        return Task.FromResult(new AuthenticationState(principal));
    }

    public void SetIdentityMode(DevIdentityMode mode)
    {
        _currentMode = mode;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}

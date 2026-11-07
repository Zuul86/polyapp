namespace Dashboard.Shared.Models;

/// <summary>
/// Authentication and authorization claim constants shared between the Blazor client and API.
/// </summary>
public static class AuthConstants
{
    public const string RoleClaimType = "https://polyapp.dashboard/roles";
    public const string EmailClaimType = "email";
    public const string AdminRole = "admin";
    public const string OwnerPolicyName = "OwnerOnly";
}

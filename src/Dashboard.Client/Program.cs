using System.Security.Claims;
using Dashboard.Client;
using Dashboard.Client.Auth;
using Dashboard.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
var resolvedApiUri = !string.IsNullOrWhiteSpace(apiBaseUrl)
    ? new Uri(apiBaseUrl.TrimEnd('/') + "/")
    : new Uri(builder.HostEnvironment.BaseAddress);

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = resolvedApiUri });
builder.Services.AddScoped<DashboardApiClient>();

var auth0Options = new Auth0Options();
builder.Configuration.GetSection(Auth0Options.SectionName).Bind(auth0Options);
builder.Services.AddSingleton(auth0Options);

if (auth0Options.IsConfiguredForLiveOidc)
{
    builder.Services.AddOidcAuthentication(options =>
    {
        builder.Configuration.Bind(Auth0Options.SectionName, options.ProviderOptions);
        options.ProviderOptions.ResponseType = "code";
        options.ProviderOptions.DefaultScopes.Add("openid");
        options.ProviderOptions.DefaultScopes.Add("profile");
        options.ProviderOptions.DefaultScopes.Add("email");
        options.UserOptions.RoleClaim = "roles";
    });
}
else
{
    builder.Services.AddSingleton<DevAuthStateProvider>();
    builder.Services.AddSingleton<AuthenticationStateProvider>(sp => sp.GetRequiredService<DevAuthStateProvider>());
}

builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("PrivateAccess", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            var email = context.User.FindFirst(ClaimTypes.Email)?.Value
                        ?? context.User.FindFirst("email")?.Value;

            if (!string.IsNullOrWhiteSpace(email) &&
                auth0Options.AllowedEmails.Any(allowed => string.Equals(allowed, email, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(auth0Options.RequiredRole) &&
                (context.User.IsInRole(auth0Options.RequiredRole) ||
                 context.User.HasClaim("roles", auth0Options.RequiredRole)))
            {
                return true;
            }

            return false;
        });
    });
});

await builder.Build().RunAsync();

using System.Diagnostics;
using System.Net.Http.Json;
using Dashboard.Shared.Models;

namespace Dashboard.Client.Services;

public sealed record HealthProbeResult(
    bool IsOnline,
    long LatencyMs,
    HealthStatusDto? Payload,
    string? ErrorMessage);

public sealed class DashboardApiClient(HttpClient httpClient)
{
    private static readonly IReadOnlyList<WidgetConfigDto> DefaultSeedWidgets =
    [
        new WidgetConfigDto
        {
            Id = "widget-token-gen",
            Title = "3D Token Generator",
            Description = "Parametric coin & token designer with curved rim text, dual-sided relief, and split-body STL export.",
            Url = "/tools/token-generator",
            Icon = "bi-box",
            Category = "Mini-App",
            IsPrivate = false,
            IsInternal = true,
            SortOrder = 1
        },
        new WidgetConfigDto
        {
            Id = "widget-api-health",
            Title = "Azure Functions API",
            Description = "Direct JSON health endpoint running on .NET 10 Isolated Worker.",
            Url = "http://localhost:7071/api/health",
            Icon = "bi-lightning-charge",
            Category = "Backend",
            IsPrivate = false,
            IsInternal = false,
            SortOrder = 2
        },
        new WidgetConfigDto
        {
            Id = "widget-github-repo",
            Title = "MiniAppHub Repository",
            Description = "Source code, Bicep infrastructure, and GitHub Actions pipelines.",
            Url = "https://github.com/Zuul86/polyapp",
            Icon = "bi-github",
            Category = "DevOps",
            IsPrivate = false,
            IsInternal = false,
            SortOrder = 3
        },
        new WidgetConfigDto
        {
            Id = "widget-home-assistant",
            Title = "Home Assistant & BamBuddy",
            Description = "Private smart-home automation hub and Bambu Lab A1 MQTT bridge.",
            Url = "http://homeassistant.local:8123",
            Icon = "bi-house-gear",
            Category = "Self-Hosted",
            IsPrivate = true,
            IsInternal = false,
            SortOrder = 10
        },
        new WidgetConfigDto
        {
            Id = "widget-cosmos-explorer",
            Title = "Cosmos DB Data Explorer",
            Description = "Inspect UserItems partitions (tokens, widgets, print-history) in Azure Portal.",
            Url = "https://cosmos.azure.com",
            Icon = "bi-database-lock",
            Category = "Cloud Infra",
            IsPrivate = true,
            IsInternal = false,
            SortOrder = 11
        },
        new WidgetConfigDto
        {
            Id = "widget-portainer",
            Title = "Homelab Container Stack",
            Description = "Private self-hosted container management and LAN reverse proxy.",
            Url = "https://portainer.local:9443",
            Icon = "bi-hdd-network",
            Category = "Self-Hosted",
            IsPrivate = true,
            IsInternal = false,
            SortOrder = 12
        }
    ];

    public async Task<HealthProbeResult> ProbeHealthAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var payload = await httpClient.GetFromJsonAsync<HealthStatusDto>("api/health", cancellationToken);
            sw.Stop();
            return new HealthProbeResult(
                IsOnline: payload is not null,
                LatencyMs: sw.ElapsedMilliseconds,
                Payload: payload,
                ErrorMessage: null);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new HealthProbeResult(
                IsOnline: false,
                LatencyMs: sw.ElapsedMilliseconds,
                Payload: null,
                ErrorMessage: ex.Message);
        }
    }

    public Task<IReadOnlyList<WidgetConfigDto>> GetWidgetsAsync(bool includePrivate, CancellationToken cancellationToken = default)
    {
        var filtered = DefaultSeedWidgets
            .Where(w => includePrivate || !w.IsPrivate)
            .OrderBy(w => w.SortOrder)
            .ToList();

        return Task.FromResult<IReadOnlyList<WidgetConfigDto>>(filtered);
    }
}

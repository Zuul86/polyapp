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
    private static readonly IReadOnlyList<WidgetConfigDto> OfflineFallbackWidgets =
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

    public async Task<IReadOnlyList<WidgetConfigDto>> GetWidgetsAsync(bool includePrivate, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"api/items/{PartitionKeys.Widgets}?includePrivate={includePrivate.ToString().ToLowerInvariant()}";
            var items = await httpClient.GetFromJsonAsync<List<WidgetConfigDto>>(url, cancellationToken);
            if (items is not null)
            {
                return items
                    .Where(w => includePrivate || !w.IsPrivate)
                    .OrderBy(w => w.SortOrder)
                    .ToList();
            }
        }
        catch
        {
            // Fallback to offline defaults if API host is not running
        }

        return OfflineFallbackWidgets
            .Where(w => includePrivate || !w.IsPrivate)
            .OrderBy(w => w.SortOrder)
            .ToList();
    }

    public async Task<WidgetConfigDto?> SaveWidgetAsync(WidgetConfigDto widget, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync($"api/items/{PartitionKeys.Widgets}", widget, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WidgetConfigDto>(cancellationToken: cancellationToken);
    }

    public async Task<bool> DeleteWidgetAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"api/items/{PartitionKeys.Widgets}/{Uri.EscapeDataString(id)}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<IReadOnlyList<T>> GetItemsAsync<T>(string partitionKey, CancellationToken cancellationToken = default)
    {
        var items = await httpClient.GetFromJsonAsync<List<T>>($"api/items/{partitionKey}", cancellationToken);
        return items ?? [];
    }

    public async Task<T?> UpsertItemAsync<T>(string partitionKey, T item, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync($"api/items/{partitionKey}", item, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }

    public async Task<bool> DeleteItemAsync(string partitionKey, string id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"api/items/{partitionKey}/{Uri.EscapeDataString(id)}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<PrintMonitorFeedDto> GetPrintMonitorFeedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var feed = await httpClient.GetFromJsonAsync<PrintMonitorFeedDto>("api/print-status", cancellationToken);
            if (feed is not null)
            {
                return feed;
            }
        }
        catch
        {
            // Fallback when API host is offline
        }

        var fallbackCurrent = new PrintStatusDto
        {
            Id = "print-latest",
            PrinterName = "Bambu Lab A1",
            State = "Printing",
            JobName = "Dual_Split_Token_40mm_PLA.3mf",
            ProgressPercent = 68,
            CurrentLayer = 14,
            TotalLayers = 20,
            RemainingMinutes = 11,
            NozzleTempCelsius = 220.0,
            BedTempCelsius = 60.0,
            FilamentSlots =
            [
                new FilamentSlotDto { SlotIndex = 1, Material = "PLA Basic", ColorHex = "#00AE42", IsActive = true },
                new FilamentSlotDto { SlotIndex = 2, Material = "PLA Matte", ColorHex = "#1E293B", IsActive = false },
                new FilamentSlotDto { SlotIndex = 3, Material = "PETG HF", ColorHex = "#38BDF8", IsActive = false },
                new FilamentSlotDto { SlotIndex = 4, Material = "PLA Silk", ColorHex = "#F59E0B", IsActive = false }
            ]
        };

        return new PrintMonitorFeedDto
        {
            Current = fallbackCurrent,
            RecentJobs =
            [
                new PrintStatusDto
                {
                    Id = "job-history-01",
                    PrinterName = "Bambu Lab A1",
                    State = "Finished",
                    JobName = "Alignment_Pins_Test_v2.3mf",
                    ProgressPercent = 100,
                    CurrentLayer = 12,
                    TotalLayers = 12,
                    RemainingMinutes = 0,
                    UpdatedAtUtc = DateTimeOffset.UtcNow.AddHours(-3)
                }
            ]
        };
    }

    public async Task<PrintStatusDto?> PostPrintStatusAsync(PrintStatusDto status, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/print-status", status, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PrintStatusDto>(cancellationToken: cancellationToken);
    }
}

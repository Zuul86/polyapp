using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dashboard.Shared.Models;

namespace Dashboard.Api.Services;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IUserItemsRepository"/> used when <c>CosmosDbConnectionString</c> is not configured.
/// </summary>
public sealed class InMemoryUserItemsRepository : IUserItemsRepository
{
    private readonly ConcurrentDictionary<(string PartitionKey, string Id), string> _store = new();

    public string StorageMode => "InMemoryFallback";

    public InMemoryUserItemsRepository()
    {
        SeedDefaults();
    }

    public Task<IReadOnlyList<JsonElement>> ListByPartitionAsync(
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        var normalizedPk = partitionKey.ToLowerInvariant();
        var items = _store
            .Where(kvp => string.Equals(kvp.Key.PartitionKey, normalizedPk, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => ParseClone(kvp.Value))
            .OrderBy(el => el.TryGetProperty("sortOrder", out var sortProp) && sortProp.TryGetInt32(out var sortVal) ? sortVal : int.MaxValue)
            .ThenByDescending(el => el.TryGetProperty("updatedAtUtc", out var updatedProp) ? updatedProp.GetString() : string.Empty)
            .ToList();

        return Task.FromResult<IReadOnlyList<JsonElement>>(items);
    }

    public Task<JsonElement?> GetByIdAsync(
        string partitionKey,
        string id,
        CancellationToken cancellationToken = default)
    {
        var key = (partitionKey.ToLowerInvariant(), id);
        if (_store.TryGetValue(key, out var rawJson))
        {
            return Task.FromResult<JsonElement?>(ParseClone(rawJson));
        }

        return Task.FromResult<JsonElement?>(null);
    }

    public Task<JsonElement> UpsertAsync(
        string partitionKey,
        JsonElement item,
        CancellationToken cancellationToken = default)
    {
        var normalizedPk = partitionKey.ToLowerInvariant();
        var normalizedJson = NormalizeDocumentJson(normalizedPk, item, out var id);
        _store[(normalizedPk, id)] = normalizedJson;
        return Task.FromResult(ParseClone(normalizedJson));
    }

    public Task<bool> DeleteAsync(
        string partitionKey,
        string id,
        CancellationToken cancellationToken = default)
    {
        var removed = _store.TryRemove((partitionKey.ToLowerInvariant(), id), out _);
        return Task.FromResult(removed);
    }

    internal static string NormalizeDocumentJson(string partitionKey, JsonElement item, out string id)
    {
        var node = JsonNode.Parse(item.GetRawText()) as JsonObject
                   ?? throw new InvalidOperationException("Document payload must be a JSON object.");

        if (node["id"] is JsonValue idVal && idVal.TryGetValue<string>(out var existingId) && !string.IsNullOrWhiteSpace(existingId))
        {
            id = existingId;
        }
        else
        {
            id = Guid.NewGuid().ToString("N");
            node["id"] = id;
        }

        node["partitionKey"] = partitionKey;
        node["updatedAtUtc"] = DateTimeOffset.UtcNow.ToString("O");

        return node.ToJsonString();
    }

    private static JsonElement ParseClone(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private void SeedDefaults()
    {
        WidgetConfigDto[] seedWidgets =
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

        foreach (var widget in seedWidgets)
        {
            var json = JsonSerializer.Serialize(widget);
            _store[(PartitionKeys.Widgets, widget.Id)] = json;
        }

        var starterPreset = new TokenPresetDto
        {
            Id = "preset-default-commemorative",
            Name = "Default Commemorative Coin (40mm)",
            DiameterMm = 40.0,
            ThicknessMm = 4.0,
            RimWidthMm = 3.0,
            RimReliefMm = 0.8,
            TopRimText = "MINI APP HUB",
            BottomRimText = "DOTNET 10 EDITION",
            TextSizeMm = 2.4,
            TextDepthMm = 0.6
        };
        _store[(PartitionKeys.Tokens, starterPreset.Id)] = JsonSerializer.Serialize(starterPreset);

        var samplePrint = new PrintStatusDto
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
        _store[(PartitionKeys.PrintHistory, samplePrint.Id)] = JsonSerializer.Serialize(samplePrint);
    }
}

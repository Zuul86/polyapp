using System.Text.Json.Serialization;

namespace Dashboard.Shared.Models;

/// <summary>
/// Represents the operational state returned by <c>GET /api/health</c>.
/// </summary>
public sealed record HealthStatusDto
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = "Healthy";

    [JsonPropertyName("service")]
    public string Service { get; init; } = "Dashboard.Api";

    [JsonPropertyName("framework")]
    public string Framework { get; init; } = ".NET 10.0";

    [JsonPropertyName("storageMode")]
    public string StorageMode { get; init; } = "InMemoryFallback";

    [JsonPropertyName("timestampUtc")]
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("uptimeSeconds")]
    public long UptimeSeconds { get; init; }
}

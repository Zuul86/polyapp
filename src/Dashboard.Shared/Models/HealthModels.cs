using System.Text.Json.Serialization;

namespace Dashboard.Shared.Models;

/// <summary>
/// Operational health state of a backend service or dependency.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ServiceOperationalState
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}

/// <summary>
/// Health status for an individual downstream dependency (e.g. Cosmos DB, MQTT bridge).
/// </summary>
public sealed record DependencyHealthDto
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("state")]
    public ServiceOperationalState State { get; init; } = ServiceOperationalState.Healthy;

    [JsonPropertyName("latencyMs")]
    public double LatencyMs { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}

/// <summary>
/// Response payload returned by the Azure Function health endpoint.
/// </summary>
public sealed record ServiceHealthDto
{
    [JsonPropertyName("serviceName")]
    public string ServiceName { get; init; } = "Dashboard.Api";

    [JsonPropertyName("state")]
    public ServiceOperationalState State { get; init; } = ServiceOperationalState.Healthy;

    [JsonPropertyName("version")]
    public string Version { get; init; } = "1.0.0";

    [JsonPropertyName("timestampUtc")]
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("uptimeSeconds")]
    public long UptimeSeconds { get; init; }

    [JsonPropertyName("serverLatencyMs")]
    public double ServerLatencyMs { get; init; }

    [JsonPropertyName("dependencies")]
    public IReadOnlyList<DependencyHealthDto> Dependencies { get; init; } = [];
}

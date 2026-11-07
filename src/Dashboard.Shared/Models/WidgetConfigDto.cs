using System.Text.Json.Serialization;

namespace Dashboard.Shared.Models;

/// <summary>
/// Configuration for a Quick-Launcher tile or dashboard widget stored under partitionKey <c>"widgets"</c>.
/// </summary>
public sealed record WidgetConfigDto : CosmosDocument
{
    [JsonPropertyName("partitionKey")]
    public override string PartitionKey { get; init; } = PartitionKeys.Widgets;

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("icon")]
    public string Icon { get; init; } = "bi-box-arrow-up-right";

    [JsonPropertyName("category")]
    public string Category { get; init; } = "General";

    [JsonPropertyName("isPrivate")]
    public bool IsPrivate { get; init; }

    [JsonPropertyName("isInternal")]
    public bool IsInternal { get; init; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; init; }
}

using System.Text.Json.Serialization;

namespace Dashboard.Shared.Models;

/// <summary>
/// Represents a single tile in the Mini-App Quick Launcher widget.
/// </summary>
public sealed record QuickLaunchTileDto
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("icon")]
    public string Icon { get; init; } = "bi-app-indicator";

    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("category")]
    public string Category { get; init; } = "Tools";

    [JsonPropertyName("isInternal")]
    public bool IsInternal { get; init; } = true;

    [JsonPropertyName("requiresAuth")]
    public bool RequiresAuth { get; init; }

    [JsonPropertyName("order")]
    public int Order { get; init; }

    [JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; init; } = [];
}

/// <summary>
/// Placement and visibility settings for an individual dashboard widget.
/// </summary>
public sealed record WidgetPlacementDto
{
    [JsonPropertyName("widgetId")]
    public required string WidgetId { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("columnSpan")]
    public int ColumnSpan { get; init; } = 1;

    [JsonPropertyName("order")]
    public int Order { get; init; }

    [JsonPropertyName("isVisible")]
    public bool IsVisible { get; init; } = true;

    [JsonPropertyName("requiresAuth")]
    public bool RequiresAuth { get; init; }
}

/// <summary>
/// Persisted widget layout and quick-launcher configuration stored in Cosmos DB (<c>partitionKey = "widgets"</c>).
/// </summary>
public sealed record WidgetLayoutDocument : UserItemDocument
{
    public WidgetLayoutDocument()
    {
        PartitionKey = CosmosConstants.PartitionKeys.Widgets;
    }

    [JsonPropertyName("widgets")]
    public IReadOnlyList<WidgetPlacementDto> Widgets { get; init; } = [];

    [JsonPropertyName("quickLaunchTiles")]
    public IReadOnlyList<QuickLaunchTileDto> QuickLaunchTiles { get; init; } = [];
}

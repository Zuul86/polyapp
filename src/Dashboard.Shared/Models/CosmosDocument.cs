using System.Text.Json.Serialization;

namespace Dashboard.Shared.Models;

/// <summary>
/// Well-known partition keys for the Cosmos DB <c>UserItems</c> container in <c>DashboardDb</c>.
/// </summary>
public static class PartitionKeys
{
    public const string Tokens = "tokens";
    public const string Widgets = "widgets";
    public const string PrintHistory = "print-history";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Tokens,
        Widgets,
        PrintHistory
    };
}

/// <summary>
/// Base contract for documents stored in the Cosmos DB <c>UserItems</c> container.
/// </summary>
public abstract record CosmosDocument
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("partitionKey")]
    public abstract string PartitionKey { get; init; }

    [JsonPropertyName("ownerEmail")]
    public string? OwnerEmail { get; init; }

    [JsonPropertyName("updatedAtUtc")]
    public DateTimeOffset UpdatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}

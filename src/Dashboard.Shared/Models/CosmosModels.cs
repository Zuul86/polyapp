using System.Text.Json.Serialization;

namespace Dashboard.Shared.Models;

/// <summary>
/// Cosmos DB container and partition key constants as defined in the project specification.
/// </summary>
public static class CosmosConstants
{
    public const string DatabaseName = "DashboardDb";
    public const string ContainerName = "UserItems";
    public const string PartitionKeyPath = "/partitionKey";

    public static class PartitionKeys
    {
        public const string Tokens = "tokens";
        public const string Widgets = "widgets";
        public const string PrintHistory = "print-history";
    }
}

/// <summary>
/// Base contract for items stored in the Cosmos DB <c>UserItems</c> container.
/// </summary>
public abstract record UserItemDocument
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("partitionKey")]
    public required string PartitionKey { get; init; }

    [JsonPropertyName("userId")]
    public string? UserId { get; init; }

    [JsonPropertyName("createdAtUtc")]
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("updatedAtUtc")]
    public DateTimeOffset UpdatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}

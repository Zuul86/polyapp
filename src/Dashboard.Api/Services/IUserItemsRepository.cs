using System.Text.Json;

namespace Dashboard.Api.Services;

/// <summary>
/// Abstraction over the Cosmos DB <c>UserItems</c> container (partitioned by <c>/partitionKey</c>).
/// </summary>
public interface IUserItemsRepository
{
    string StorageMode { get; }

    Task<IReadOnlyList<JsonElement>> ListByPartitionAsync(
        string partitionKey,
        CancellationToken cancellationToken = default);

    Task<JsonElement?> GetByIdAsync(
        string partitionKey,
        string id,
        CancellationToken cancellationToken = default);

    Task<JsonElement> UpsertAsync(
        string partitionKey,
        JsonElement item,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string partitionKey,
        string id,
        CancellationToken cancellationToken = default);
}

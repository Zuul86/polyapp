using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Cosmos;

namespace Dashboard.Api.Services;

/// <summary>
/// Azure Cosmos DB for NoSQL implementation of <see cref="IUserItemsRepository"/> targeting
/// database <c>DashboardDb</c> and container <c>UserItems</c> (partition key <c>/partitionKey</c>).
/// </summary>
public sealed class CosmosUserItemsRepository : IUserItemsRepository
{
    public const string DatabaseName = "DashboardDb";
    public const string ContainerName = "UserItems";

    private readonly Container _container;

    public string StorageMode => "CosmosDb";

    public CosmosUserItemsRepository(CosmosClient cosmosClient)
    {
        _container = cosmosClient.GetContainer(DatabaseName, ContainerName);
    }

    public async Task<IReadOnlyList<JsonElement>> ListByPartitionAsync(
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        var normalizedPk = partitionKey.ToLowerInvariant();
        var query = new QueryDefinition("SELECT * FROM c WHERE c.partitionKey = @pk")
            .WithParameter("@pk", normalizedPk);

        var requestOptions = new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(normalizedPk)
        };

        var results = new List<JsonElement>();
        using var iterator = _container.GetItemQueryStreamIterator(query, requestOptions: requestOptions);

        while (iterator.HasMoreResults)
        {
            using var response = await iterator.ReadNextAsync(cancellationToken);
            response.EnsureSuccessStatusCode();

            using var doc = await JsonDocument.ParseAsync(response.Content, cancellationToken: cancellationToken);
            if (doc.RootElement.TryGetProperty("Documents", out var documentsArray) &&
                documentsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in documentsArray.EnumerateArray())
                {
                    results.Add(item.Clone());
                }
            }
        }

        return results;
    }

    public async Task<JsonElement?> GetByIdAsync(
        string partitionKey,
        string id,
        CancellationToken cancellationToken = default)
    {
        var normalizedPk = partitionKey.ToLowerInvariant();
        using var response = await _container.ReadItemStreamAsync(
            id,
            new PartitionKey(normalizedPk),
            cancellationToken: cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(response.Content, cancellationToken: cancellationToken);
        return doc.RootElement.Clone();
    }

    public async Task<JsonElement> UpsertAsync(
        string partitionKey,
        JsonElement item,
        CancellationToken cancellationToken = default)
    {
        var normalizedPk = partitionKey.ToLowerInvariant();
        var normalizedJson = InMemoryUserItemsRepository.NormalizeDocumentJson(normalizedPk, item, out _);

        using var payloadStream = new MemoryStream(Encoding.UTF8.GetBytes(normalizedJson));
        using var response = await _container.UpsertItemStreamAsync(
            payloadStream,
            new PartitionKey(normalizedPk),
            cancellationToken: cancellationToken);

        response.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(response.Content, cancellationToken: cancellationToken);
        return doc.RootElement.Clone();
    }

    public async Task<bool> DeleteAsync(
        string partitionKey,
        string id,
        CancellationToken cancellationToken = default)
    {
        var normalizedPk = partitionKey.ToLowerInvariant();
        using var response = await _container.DeleteItemStreamAsync(
            id,
            new PartitionKey(normalizedPk),
            cancellationToken: cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }
}

using System.Text.Json;
using Dashboard.Api.Auth;
using Dashboard.Api.Services;
using Dashboard.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Dashboard.Api.Functions;

public sealed class UserItemsFunctions(
    IUserItemsRepository repository,
    ApiAuthGuard authGuard,
    ILogger<UserItemsFunctions> logger)
{
    [Function("ListUserItems")]
    public async Task<IActionResult> ListByPartitionAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "items/{partitionKey}")] HttpRequest req,
        string partitionKey,
        CancellationToken cancellationToken)
    {
        if (!PartitionKeys.All.Contains(partitionKey))
        {
            return new BadRequestObjectResult(new { error = $"Invalid partitionKey '{partitionKey}'. Expected one of: {string.Join(", ", PartitionKeys.All)}" });
        }

        var items = await repository.ListByPartitionAsync(partitionKey, cancellationToken);

        if (string.Equals(partitionKey, PartitionKeys.Widgets, StringComparison.OrdinalIgnoreCase) &&
            req.Query.TryGetValue("includePrivate", out var includePrivateVal) &&
            bool.TryParse(includePrivateVal, out var includePrivate) &&
            !includePrivate)
        {
            items = items
                .Where(el => !(el.TryGetProperty("isPrivate", out var privProp) && privProp.ValueKind == JsonValueKind.True))
                .ToList();
        }

        return new OkObjectResult(items);
    }

    [Function("GetUserItemById")]
    public async Task<IActionResult> GetByIdAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "items/{partitionKey}/{id}")] HttpRequest req,
        string partitionKey,
        string id,
        CancellationToken cancellationToken)
    {
        if (!PartitionKeys.All.Contains(partitionKey))
        {
            return new BadRequestObjectResult(new { error = $"Invalid partitionKey '{partitionKey}'." });
        }

        var item = await repository.GetByIdAsync(partitionKey, id, cancellationToken);
        if (item is null)
        {
            return new NotFoundObjectResult(new { error = $"Item '{id}' not found in partition '{partitionKey}'." });
        }

        return new OkObjectResult(item.Value);
    }

    [Function("UpsertUserItem")]
    public async Task<IActionResult> UpsertAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "items/{partitionKey}")] HttpRequest req,
        string partitionKey,
        CancellationToken cancellationToken)
    {
        if (!PartitionKeys.All.Contains(partitionKey))
        {
            return new BadRequestObjectResult(new { error = $"Invalid partitionKey '{partitionKey}'." });
        }

        if (!authGuard.IsWriteAuthorized(req, out var denialReason))
        {
            return new ObjectResult(new { error = denialReason }) { StatusCode = StatusCodes.Status401Unauthorized };
        }

        JsonDocument doc;
        try
        {
            doc = await JsonDocument.ParseAsync(req.Body, cancellationToken: cancellationToken);
        }
        catch (JsonException ex)
        {
            return new BadRequestObjectResult(new { error = $"Invalid JSON body: {ex.Message}" });
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new BadRequestObjectResult(new { error = "Request body must be a JSON object." });
            }

            var saved = await repository.UpsertAsync(partitionKey, doc.RootElement, cancellationToken);
            logger.LogInformation("Upserted item in partition {PartitionKey}", partitionKey);
            return new OkObjectResult(saved);
        }
    }

    [Function("DeleteUserItem")]
    public async Task<IActionResult> DeleteAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "items/{partitionKey}/{id}")] HttpRequest req,
        string partitionKey,
        string id,
        CancellationToken cancellationToken)
    {
        if (!PartitionKeys.All.Contains(partitionKey))
        {
            return new BadRequestObjectResult(new { error = $"Invalid partitionKey '{partitionKey}'." });
        }

        if (!authGuard.IsWriteAuthorized(req, out var denialReason))
        {
            return new ObjectResult(new { error = denialReason }) { StatusCode = StatusCodes.Status401Unauthorized };
        }

        var deleted = await repository.DeleteAsync(partitionKey, id, cancellationToken);
        if (!deleted)
        {
            return new NotFoundObjectResult(new { error = $"Item '{id}' not found in partition '{partitionKey}'." });
        }

        logger.LogInformation("Deleted item {Id} from partition {PartitionKey}", id, partitionKey);
        return new NoContentResult();
    }
}

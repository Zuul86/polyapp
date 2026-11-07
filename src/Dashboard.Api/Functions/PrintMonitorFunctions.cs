using System.Text.Json;
using Dashboard.Api.Auth;
using Dashboard.Api.Services;
using Dashboard.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Dashboard.Api.Functions;

public sealed class PrintMonitorFunctions(
    IUserItemsRepository repository,
    ApiAuthGuard authGuard,
    ILogger<PrintMonitorFunctions> logger)
{
    public const string LatestStatusDocumentId = "print-latest";

    [Function("GetPrintStatus")]
    public async Task<IActionResult> GetStatusAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "print-status")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var rawItems = await repository.ListByPartitionAsync(PartitionKeys.PrintHistory, cancellationToken);

        var parsedItems = new List<PrintStatusDto>();
        foreach (var element in rawItems)
        {
            var dto = element.Deserialize<PrintStatusDto>();
            if (dto is not null)
            {
                parsedItems.Add(dto);
            }
        }

        var current = parsedItems.FirstOrDefault(p => string.Equals(p.Id, LatestStatusDocumentId, StringComparison.OrdinalIgnoreCase))
                      ?? parsedItems.OrderByDescending(p => p.UpdatedAtUtc).FirstOrDefault();

        var recentJobs = parsedItems
            .Where(p => !string.Equals(p.Id, LatestStatusDocumentId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.UpdatedAtUtc)
            .Take(8)
            .ToList();

        var feed = new PrintMonitorFeedDto
        {
            Current = current,
            RecentJobs = recentJobs
        };

        return new OkObjectResult(feed);
    }

    [Function("IngestPrintStatus")]
    public async Task<IActionResult> IngestStatusAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "print-status")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        if (!IsWebhookOrWriteAuthorized(req, out var denialReason))
        {
            return new ObjectResult(new { error = denialReason }) { StatusCode = StatusCodes.Status401Unauthorized };
        }

        PrintStatusDto? incoming;
        try
        {
            incoming = await JsonSerializer.DeserializeAsync<PrintStatusDto>(req.Body, cancellationToken: cancellationToken);
        }
        catch (JsonException ex)
        {
            return new BadRequestObjectResult(new { error = $"Invalid JSON payload: {ex.Message}" });
        }

        if (incoming is null)
        {
            return new BadRequestObjectResult(new { error = "Request body cannot be null." });
        }

        var now = DateTimeOffset.UtcNow;

        // 1. Always upsert the live state document ("print-latest")
        var liveSnapshot = incoming with
        {
            Id = LatestStatusDocumentId,
            PartitionKey = PartitionKeys.PrintHistory,
            UpdatedAtUtc = now
        };

        using var liveDoc = JsonDocument.Parse(JsonSerializer.Serialize(liveSnapshot));
        await repository.UpsertAsync(PartitionKeys.PrintHistory, liveDoc.RootElement, cancellationToken);

        // 2. Also record a history entry if a custom id was supplied or the job finished/started
        var historyId = !string.IsNullOrWhiteSpace(incoming.Id) &&
                        !string.Equals(incoming.Id, LatestStatusDocumentId, StringComparison.OrdinalIgnoreCase)
            ? incoming.Id
            : $"job-{now:yyyyMMddHHmmss}";

        var historyEntry = incoming with
        {
            Id = historyId,
            PartitionKey = PartitionKeys.PrintHistory,
            UpdatedAtUtc = now
        };

        using var historyDoc = JsonDocument.Parse(JsonSerializer.Serialize(historyEntry));
        await repository.UpsertAsync(PartitionKeys.PrintHistory, historyDoc.RootElement, cancellationToken);

        logger.LogInformation(
            "Updated Bambu A1 status: State={State}, Job={JobName}, Progress={Progress}%",
            liveSnapshot.State,
            liveSnapshot.JobName,
            liveSnapshot.ProgressPercent);

        return new OkObjectResult(liveSnapshot);
    }

    private bool IsWebhookOrWriteAuthorized(HttpRequest req, out string? denialReason)
    {
        var expectedWebhookSecret = Environment.GetEnvironmentVariable("BambuWebhookSecret");
        if (!string.IsNullOrWhiteSpace(expectedWebhookSecret) &&
            req.Headers.TryGetValue("X-Webhook-Secret", out var providedSecret) &&
            string.Equals(expectedWebhookSecret, providedSecret.ToString(), StringComparison.Ordinal))
        {
            denialReason = null;
            return true;
        }

        return authGuard.IsWriteAuthorized(req, out denialReason);
    }
}

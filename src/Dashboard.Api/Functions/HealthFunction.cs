using Dashboard.Api.Services;
using Dashboard.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Dashboard.Api.Functions;

public sealed class HealthFunction(
    IUserItemsRepository repository,
    ILogger<HealthFunction> logger)
{
    private static readonly DateTimeOffset StartedAtUtc = DateTimeOffset.UtcNow;

    [Function("Health")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req)
    {
        logger.LogInformation("Health check requested from {RemoteIp}", req.HttpContext.Connection.RemoteIpAddress);

        var uptimeSeconds = (long)Math.Max(0, (DateTimeOffset.UtcNow - StartedAtUtc).TotalSeconds);

        var response = new HealthStatusDto
        {
            Status = "Healthy",
            Service = "Dashboard.Api",
            Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            StorageMode = repository.StorageMode,
            TimestampUtc = DateTimeOffset.UtcNow,
            UptimeSeconds = uptimeSeconds
        };

        return new OkObjectResult(response);
    }
}

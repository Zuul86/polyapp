using Azure.Monitor.OpenTelemetry.Exporter;
using Dashboard.Api.Auth;
using Dashboard.Api.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddSingleton<ApiAuthGuard>();

var cosmosConnectionString = Environment.GetEnvironmentVariable("CosmosDbConnectionString");
if (!string.IsNullOrWhiteSpace(cosmosConnectionString))
{
    builder.Services.AddSingleton(_ => new CosmosClient(cosmosConnectionString, new CosmosClientOptions
    {
        ApplicationName = "MiniAppHub.Dashboard.Api",
        ConnectionMode = ConnectionMode.Gateway
    }));
    builder.Services.AddSingleton<IUserItemsRepository, CosmosUserItemsRepository>();
}
else
{
    builder.Services.AddSingleton<IUserItemsRepository, InMemoryUserItemsRepository>();
}

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Build().Run();

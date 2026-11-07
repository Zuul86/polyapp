---
trigger: always_on
---

# Architecture & Coding Standards (MiniAppHub)

## 1. Project Boundaries (`MiniAppHub.slnx`)
- **Target Framework:** All projects target `net10.0` with `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`.
- **`src/Dashboard.Shared`:** Pure contract library (`CosmosDocument`, DTOs, `PartitionKeys`). Keep free of framework-specific dependencies (no Blazor or Azure Functions packages).
- **`src/Dashboard.Api`:** Azure Functions v4 Isolated Worker (`net10.0`). All HTTP triggers reside in `src/Dashboard.Api/Functions/` and use `IUserItemsRepository` for persistence.
- **`src/Dashboard.Client`:** Standalone Blazor WebAssembly (`net10.0`). Communicates with the backend exclusively through `DashboardApiClient` (`src/Dashboard.Client/Services/DashboardApiClient.cs`).

## 2. Persistence & Serialization Invariants
- **Single-Container Cosmos DB Design:** All persisted entities inherit from `CosmosDocument` (`id`, `partitionKey`, `updatedAtUtc`) and use logical partition keys defined in `PartitionKeys` (`"tokens"`, `"widgets"`, `"print-history"`).
- **Repository Parity:** Every feature using `IUserItemsRepository` must work identically with both `CosmosUserItemsRepository` (when `CosmosDbConnectionString` is configured) and `InMemoryUserItemsRepository` (local/CI fallback). When adding a new feature or widget type, seed sensible default development data in `InMemoryUserItemsRepository` if appropriate.
- **JSON Serialization:** Always use `System.Text.Json` attributes (`[JsonPropertyName("...")]`) on shared DTOs. `CosmosUserItemsRepository` uses Cosmos stream APIs (`ReadItemStreamAsync`, `UpsertItemStreamAsync`, `GetItemQueryStreamIterator`) to serialize and deserialize directly via `System.Text.Json` without `Newtonsoft.Json` attribute drift.

## 3. Authentication & Authorization Invariants
- **Client Auth:** `Dashboard.Client` supports Auth0 OIDC in production and `DevAuthStateProvider` when `UseDevAuthFallback: true` in `wwwroot/appsettings.json`. Do not hardcode a single authentication provider.
- **API Write Guard (`ApiAuthGuard`):** Mutating endpoints (`POST`, `PUT`, `DELETE`) in `Dashboard.Api` must validate requests via `ApiAuthGuard.IsAuthorizedForMutation(req, config)` (or API key validation for webhook/MQTT ingestion endpoints).
- **Resilient UI Fetching:** `DashboardApiClient` methods must gracefully handle offline/unreachable backend states (catching `HttpRequestException` / `JsonException`) so the Blazor WASM UI never crashes when the Functions backend is stopped.

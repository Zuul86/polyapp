# API Reference & Cosmos DB Data Model

## 1. Cosmos DB Serverless Schema

MiniAppHub uses a single-container NoSQL design in **Azure Cosmos DB** to minimize RU consumption and qualify for the Azure Cosmos DB Free Tier / Serverless model.

- **Database Name:** `DashboardDb` (configurable via `CosmosDbDatabaseName`)
- **Container Name:** `UserItems` (configurable via `CosmosDbContainerName`)
- **Partition Key Path:** `/partitionKey`

### Base Document Contract (`CosmosDocument`)
Defined in [CosmosDocument.cs](../src/Dashboard.Shared/Models/CosmosDocument.cs):
```json
{
  "id": "string",
  "partitionKey": "tokens | widgets | print-history",
  "updatedAtUtc": "2026-11-07T16:00:00Z"
}
```

### Logical Partitions (`PartitionKeys`)

| Partition Key (`partitionKey`) | Constant | DTO Model | Purpose |
| :--- | :--- | :--- | :--- |
| `"widgets"` | `PartitionKeys.Widgets` | [WidgetConfigDto](../src/Dashboard.Shared/Models/WidgetConfigDto.cs) | Quick Launcher tiles (`title`, `description`, `icon`, `targetUrl`, `category`, `isPrivateOnly`, `order`, `openInNewTab`). |
| `"print-history"` | `PartitionKeys.PrintHistory` | [PrintStatusDto](../src/Dashboard.Shared/Models/PrintStatusDto.cs) | Active Bambu Lab A1 status (`id = "bambu-a1-current"`) and historical print log records (`printerName`, `state`, `jobName`, `progressPercent`, `currentLayer`, `totalLayers`, `remainingMinutes`, `nozzleTempC`, `bedTempC`, `amsSlots`). |
| `"tokens"` | `PartitionKeys.Tokens` | [TokenPresetDto](../src/Dashboard.Shared/Models/TokenPresetDto.cs) | Saved 3D Token Generator parameter presets (`diameterMm`, `thicknessMm`, `rimWidthMm`, `topRimText`, `bottomRimText`, `frontFace`, `backFace`, `splitBody`). |

---

## 2. Repository Pattern & Serialization (`IUserItemsRepository`)

Defined in [IUserItemsRepository.cs](../src/Dashboard.Api/Services/IUserItemsRepository.cs) and registered in [Program.cs](../src/Dashboard.Api/Program.cs):

1. **`CosmosUserItemsRepository`** ([CosmosUserItemsRepository.cs](../src/Dashboard.Api/Services/CosmosUserItemsRepository.cs)):
   - Activated automatically when `CosmosDbConnectionString` is set in configuration (`local.settings.json` or Azure Function App settings).
   - Uses `Container.GetItemQueryStreamIterator`, `ReadItemStreamAsync`, `UpsertItemStreamAsync`, and `DeleteItemStreamAsync` with `System.Text.Json` `JsonDocument` / `JsonElement` so `[JsonPropertyName]` attributes on `Dashboard.Shared` models work directly without `Newtonsoft.Json` mismatches.
2. **`InMemoryUserItemsRepository`** ([InMemoryUserItemsRepository.cs](../src/Dashboard.Api/Services/InMemoryUserItemsRepository.cs)):
   - Activated automatically when `CosmosDbConnectionString` is empty or omitted.
   - Pre-seeded in `SeedDefaults()` with starter Quick Launcher tiles, a default 3D token preset (`preset-arcane-coin`), an active Bambu Lab A1 print state (`bambu-a1-current`), and recent print history (`bambu-a1-hist-1`, `bambu-a1-hist-2`).

---

## 3. Azure Functions HTTP Endpoints (`src/Dashboard.Api`)

### `GET /api/health`
- **Function:** [HealthFunction.cs](../src/Dashboard.Api/Functions/HealthFunction.cs)
- **Auth:** Anonymous
- **Response (`200 OK` — [HealthStatusDto](../src/Dashboard.Shared/Models/HealthStatusDto.cs)):**
  ```json
  {
    "status": "Healthy",
    "service": "Dashboard.Api",
    "framework": ".NET 10.0.x",
    "cosmosConfigured": false,
    "storageProvider": "InMemoryFallback",
    "timestampUtc": "2026-11-07T16:00:00Z"
  }
  ```

### `GET /api/items/{partitionKey}`
- **Function:** `GetItemsByPartition` in [UserItemsFunctions.cs](../src/Dashboard.Api/Functions/UserItemsFunctions.cs)
- **Route Parameters:** `partitionKey` (`tokens`, `widgets`, or `print-history`)
- **Auth:** Anonymous
- **Response (`200 OK`):** JSON array of documents in that partition ordered by `updatedAtUtc DESC`. Returns `400 Bad Request` if `partitionKey` is not in `PartitionKeys.IsValid`.

### `GET /api/items/{partitionKey}/{id}`
- **Function:** `GetItemById` in [UserItemsFunctions.cs](../src/Dashboard.Api/Functions/UserItemsFunctions.cs)
- **Auth:** Anonymous
- **Response:** `200 OK` with the JSON document, or `404 Not Found`.

### `POST /api/items/{partitionKey}`
- **Function:** `UpsertItem` in [UserItemsFunctions.cs](../src/Dashboard.Api/Functions/UserItemsFunctions.cs)
- **Auth:** Requires `Authorization: Bearer <token>` or `X-Dev-Auth: true` (when `AllowDevAuthBypass` is enabled).
- **Behavior:** Ensures `id` (auto-generated GUID if missing), sets `partitionKey` from the route, stamps `updatedAtUtc`, and upserts into `IUserItemsRepository`.
- **Response:** `200 OK` with the persisted JSON document.

### `DELETE /api/items/{partitionKey}/{id}`
- **Function:** `DeleteItem` in [UserItemsFunctions.cs](../src/Dashboard.Api/Functions/UserItemsFunctions.cs)
- **Auth:** Requires `Authorization: Bearer <token>` or `X-Dev-Auth: true`.
- **Response:** `204 No Content` if deleted, or `404 Not Found`.

### `GET /api/print-status`
- **Function:** `GetPrintStatus` in [PrintMonitorFunctions.cs](../src/Dashboard.Api/Functions/PrintMonitorFunctions.cs)
- **Auth:** Anonymous
- **Response (`200 OK` — `PrintMonitorFeedDto` in [PrintStatusDto.cs](../src/Dashboard.Shared/Models/PrintStatusDto.cs)):**
  - `current`: The latest active print status (`id = "bambu-a1-current"`).
  - `recentJobs`: Up to 5 historical print job snapshots from `"print-history"`.

### `POST /api/print-status`
- **Function:** `IngestPrintStatus` in [PrintMonitorFunctions.cs](../src/Dashboard.Api/Functions/PrintMonitorFunctions.cs)
- **Query Params:** Optional `?archiveHistory=true` to also write a timestamped historical job snapshot (`bambu-a1-job-...`).
- **Auth:** Requires `X-Print-Api-Key: <PrintMonitorApiKey>`, `Authorization: Bearer <token>`, or `X-Dev-Auth: true`.
- **Body:** `PrintStatusDto` JSON payload from Home Assistant / BamBuddy MQTT webhook or the dashboard's **Simulate MQTT Push** action.

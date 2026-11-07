---
name: scaffold-widget-or-miniapp
description: >-
  Use this skill when adding a new Dashboard Widget, Cosmos DB partition/DTO
  contract, Azure Function endpoint, or client-side Mini-App page (including
  JavaScript ES module interop) in MiniAppHub.
---

# Scaffolding a New Widget or Mini-App

Follow these steps to add a new widget or mini-app cleanly across all three projects (`Dashboard.Shared`, `Dashboard.Api`, `Dashboard.Client`).

## 1. Define Shared Contracts (`src/Dashboard.Shared`)

1. If the feature persists items in Cosmos DB `UserItems`, check or add the partition key constant in [CosmosDocument.cs](../../../src/Dashboard.Shared/Models/CosmosDocument.cs) (`PartitionKeys`) and update `PartitionKeys.IsValid(...)`.
2. Create the DTO class in `src/Dashboard.Shared/Models/`:
   - Inherit from `CosmosDocument` if stored in `UserItems`.
   - Override `PartitionKey { get; set; } = PartitionKeys.<YourPartition>;`.
   - Annotate properties with `[JsonPropertyName("camelCaseName")]` (`System.Text.Json.Serialization`).

## 2. Expose Backend Endpoints or Seed Data (`src/Dashboard.Api`)

1. **Generic CRUD (`/api/items/{partitionKey}`)**:
   - Once a partition key is added to `PartitionKeys.IsValid`, [UserItemsFunctions.cs](../../../src/Dashboard.Api/Functions/UserItemsFunctions.cs) automatically supports `GET /api/items/{partitionKey}`, `GET /api/items/{partitionKey}/{id}`, `POST /api/items/{partitionKey}`, and `DELETE /api/items/{partitionKey}/{id}`.
2. **Custom Endpoints (if needed)**:
   - Add a new function class under `src/Dashboard.Api/Functions/` injecting `IUserItemsRepository` and `IConfiguration`.
   - Guard mutating operations with `ApiAuthGuard.IsAuthorizedForMutation(req, _configuration)`.
3. **In-Memory Development Seed Data**:
   - Update `SeedDefaults()` in [InMemoryUserItemsRepository.cs](../../../src/Dashboard.Api/Services/InMemoryUserItemsRepository.cs) so local development works out of the box without requiring a live Cosmos DB connection string.

## 3. Add Client API Methods (`src/Dashboard.Client/Services`)

1. Add strongly typed methods to [DashboardApiClient.cs](../../../src/Dashboard.Client/Services/DashboardApiClient.cs).
2. Wrap HTTP calls in `try`/`catch` blocks (`HttpRequestException`, `JsonException`) and return safe fallback values so the UI stays functional when offline.

## 4. Build the UI Component or Mini-App Page (`src/Dashboard.Client`)

- **For a Dashboard Widget**:
  1. Create `src/Dashboard.Client/Components/Widgets/<WidgetName>Widget.razor`.
  2. Embed it in [Home.razor](../../../src/Dashboard.Client/Pages/Home.razor) and/or [PrivateDashboard.razor](../../../src/Dashboard.Client/Pages/PrivateDashboard.razor).
  3. Support an `IsPrivateView` (`[Parameter] public bool IsPrivateView { get; set; }`) parameter if write/admin actions should only appear in the authenticated private workspace.
- **For a Mini-App Page (`/tools/<slug>`)**:
  1. Create `src/Dashboard.Client/Pages/<MiniAppName>.razor` with `@page "/tools/<slug>"`.
  2. If the mini-app uses browser JS libraries (e.g., `Three.js`), create an ES module in `src/Dashboard.Client/wwwroot/js/<moduleName>.js` and a matching `IAsyncDisposable` wrapper service in `src/Dashboard.Client/Services/` using `IJSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/<moduleName>.js")`.
  3. Add a navigation link in [NavMenu.razor](../../../src/Dashboard.Client/Layout/NavMenu.razor) and a default launcher tile in [InMemoryUserItemsRepository.cs](../../../src/Dashboard.Api/Services/InMemoryUserItemsRepository.cs).

## 5. Validate Build

Run the zero-warning Release build check:
```bash
dotnet build MiniAppHub.slnx --configuration Release -warnaserror
```

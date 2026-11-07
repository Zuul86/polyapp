# Adding Mini-Apps & Dashboard Widgets

This guide explains how agents should extend **MiniAppHub** with new dashboard widgets or standalone client-side mini-apps.

---

## Part 1: Adding a New Dashboard Widget

Dashboard widgets live in `src/Dashboard.Client/Components/Widgets/` and are rendered inside the CSS Grid on [Home.razor](../src/Dashboard.Client/Pages/Home.razor) (Public View) and [PrivateDashboard.razor](../src/Dashboard.Client/Pages/PrivateDashboard.razor) (Private View).

### Step-by-Step Checklist
1. **Shared DTO (`src/Dashboard.Shared/Models/`)**:
   - Create or update a DTO in `src/Dashboard.Shared/Models/`.
   - If persisted in `UserItems`, inherit from [CosmosDocument](../src/Dashboard.Shared/Models/CosmosDocument.cs) and set `PartitionKey` to an existing or new constant in `PartitionKeys`.
2. **Backend Endpoint & Seed Data (`src/Dashboard.Api/`)**:
   - If using standard CRUD on a `UserItems` partition, [UserItemsFunctions.cs](../src/Dashboard.Api/Functions/UserItemsFunctions.cs) handles `GET`, `POST`, and `DELETE` automatically once the partition key is registered in `PartitionKeys.IsValid`.
   - Add default seed items in `SeedDefaults()` inside [InMemoryUserItemsRepository.cs](../src/Dashboard.Api/Services/InMemoryUserItemsRepository.cs).
3. **Typed Client Method (`src/Dashboard.Client/Services/DashboardApiClient.cs`)**:
   - Add `Get...Async` / `Save...Async` / `Delete...Async` methods to [DashboardApiClient.cs](../src/Dashboard.Client/Services/DashboardApiClient.cs).
   - Always catch `HttpRequestException` and `JsonException` and return graceful fallback values so the widget renders cleanly even if `Dashboard.Api` is not running.
4. **Razor Widget Component (`src/Dashboard.Client/Components/Widgets/<Name>Widget.razor`)**:
   - Inject `DashboardApiClient`.
   - Accept `[Parameter] public bool IsPrivateView { get; set; }` if administrative or mutating controls should be restricted or highlighted in the Private View.
   - Follow the existing card layout classes (`dashboard-card`, `card-header`, `card-title-group`, `status-pill`) defined in [wwwroot/css/app.css](../src/Dashboard.Client/wwwroot/css/app.css).

---

## Part 2: Adding a Client-Side Mini-App (`/tools/<slug>`)

Mini-apps are interactive tools hosted at `/tools/<slug>` in `src/Dashboard.Client/Pages/`. Heavy computation (such as 3D mesh generation, CSG booleans, or file export) should run client-side in the browser via WebAssembly or JavaScript ES module interop.

### Step-by-Step Checklist
1. **Create the ES Module (`src/Dashboard.Client/wwwroot/js/<app>Interop.js`)**:
   - Export pure ES module functions (`export function initScene(...)`, `export function updateMesh(...)`, `export function dispose(...)`).
   - If importing third-party browser libraries (such as `three`, `three/addons/`, `three-bvh-csg`), declare them in the `<script type="importmap">` block inside [wwwroot/index.html](../src/Dashboard.Client/wwwroot/index.html).
2. **Create the Blazor JS Interop Service (`src/Dashboard.Client/Services/<App>JsInterop.cs`)**:
   - Implement `IAsyncDisposable`.
   - Lazily load the ES module via `IJSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/<app>Interop.js")`.
   - Dispose the `IJSObjectReference` in `DisposeAsync()` catching `JSDisconnectedException`.
   - Register the service as `Scoped` in [Program.cs](../src/Dashboard.Client/Program.cs).
3. **Create the Mini-App Page (`src/Dashboard.Client/Pages/<AppName>.razor`)**:
   - Declare `@page "/tools/<slug>"`.
   - Trigger initial JS scene setup inside `OnAfterRenderAsync(bool firstRender)` when `firstRender` is `true`.
   - Connect preset save/load functionality to `/api/items/<partitionKey>` via `DashboardApiClient`.
4. **Register Navigation & Quick Launcher Tile**:
   - Add a `NavLink` in [NavMenu.razor](../src/Dashboard.Client/Layout/NavMenu.razor).
   - Add a default `WidgetConfigDto` tile in [InMemoryUserItemsRepository.cs](../src/Dashboard.Api/Services/InMemoryUserItemsRepository.cs) pointing `TargetUrl` to `"/tools/<slug>"`.

# System Architecture (`MiniAppHub`)

## 1. High-Level Overview

**MiniAppHub** is a personal mini-app dashboard and 3D maker tool suite built on **.NET 10.0 (LTS)**. It combines a static-hosted **Blazor WebAssembly** frontend with a scale-to-zero **Azure Functions v4 (.NET 10 Isolated Worker)** backend and an **Azure Cosmos DB for NoSQL (Serverless)** persistence layer.

```
┌──────────────────────────────────────────────────────────────────────────┐
│                     Browser (Client-Side Execution)                      │
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │               src/Dashboard.Client (Blazor WASM net10.0)           │  │
│  │                                                                    │  │
│  │  • Shell Layout (Sidebar Nav + TopBar Auth State)                  │  │
│  │  • Public View ("/") & Protected Private View ("/private")         │  │
│  │  • Widgets: QuickLauncher, HealthMonitor, BambuPrintMonitor        │  │
│  │  • Mini-Apps ("/tools/token-generator")                            │  │
│  │      └── IJSObjectReference ──► Three.js + STLExporter + CSG       │  │
│  └───────────────────────────────┬────────────────────────────────────┘  │
└──────────────────────────────────┼───────────────────────────────────────┘
                                   │ HTTPS REST (JSON / System.Text.Json)
                                   ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                 src/Dashboard.Api (Azure Functions v4 net10.0)           │
│                                                                          │
│  • HealthFunction        (GET /api/health)                               │
│  • UserItemsFunctions    (GET/POST/DELETE /api/items/{partitionKey}/...) │
│  • PrintMonitorFunctions (GET/POST /api/print-status)                    │
│  • ApiAuthGuard          (Bearer JWT / X-Dev-Auth / X-Print-Api-Key)     │
└──────────────────────────────────┬───────────────────────────────────────┘
                                   │ IUserItemsRepository
                   ┌───────────────┴───────────────┐
                   ▼                               ▼
┌────────────────────────────────────┐   ┌─────────────────────────────────┐
│     CosmosUserItemsRepository      │   │   InMemoryUserItemsRepository   │
│  Azure Cosmos DB Serverless NoSQL  │   │  Automatic Local / CI Fallback  │
│  DB: DashboardDb / Col: UserItems  │   │  Pre-seeded with default items  │
└────────────────────────────────────┘   └─────────────────────────────────┘
```

---

## 2. Solution Structure (`MiniAppHub.slnx`)

| Project | Path | Target | Responsibility |
| :--- | :--- | :--- | :--- |
| **Dashboard.Shared** | [src/Dashboard.Shared/Dashboard.Shared.csproj](../src/Dashboard.Shared/Dashboard.Shared.csproj) | `net10.0` | Shared DTO contracts (`CosmosDocument`, `WidgetConfigDto`, `PrintStatusDto`, `TokenPresetDto`, `HealthStatusDto`) and `PartitionKeys` constants. Zero UI or cloud-host dependencies. |
| **Dashboard.Api** | [src/Dashboard.Api/Dashboard.Api.csproj](../src/Dashboard.Api/Dashboard.Api.csproj) | `net10.0` | Azure Functions v4 Isolated Worker API. Implements HTTP endpoints, authentication guard (`ApiAuthGuard`), and Cosmos DB / In-Memory repository implementations. |
| **Dashboard.Client** | [src/Dashboard.Client/Dashboard.Client.csproj](../src/Dashboard.Client/Dashboard.Client.csproj) | `net10.0` | Standalone Blazor WebAssembly SPA deployed to Netlify. Hosts dashboard views, widgets, Auth0 OIDC integration, and client-side WebGL/Three.js mini-apps. |

---

## 3. Authentication & Authorization Model

### Frontend Authentication (`Dashboard.Client`)
Configured in [Program.cs](../src/Dashboard.Client/Program.cs) and [wwwroot/appsettings.json](../src/Dashboard.Client/wwwroot/appsettings.json):

1. **Production Mode (Auth0 OIDC)**:
   - Uses `Microsoft.AspNetCore.Components.WebAssembly.Authentication` (`AddOidcAuthentication`) when `UseDevAuthFallback` is `false` and `Auth0:Authority` + `Auth0:ClientId` are configured.
   - Requests `openid profile email` scopes and validates the `"PrivateAccess"` authorization policy (requiring an authenticated user matching `AllowedPrivateEmail` or role `"admin"` / `"owner"`).
2. **Local Development Fallback (`DevAuthStateProvider`)**:
   - Enabled when `"UseDevAuthFallback": true` in `wwwroot/appsettings.json` (or when Auth0 credentials are placeholders).
   - Registered as [DevAuthStateProvider.cs](../src/Dashboard.Client/Auth/DevAuthStateProvider.cs), allowing instant interactive toggling in [TopBar.razor](../src/Dashboard.Client/Layout/TopBar.razor) between **Public Guest** and **Authenticated Owner (`dev-owner@local.test`)** without external OIDC redirects.

### Backend Mutation Guard (`Dashboard.Api`)
Implemented in [ApiAuthGuard.cs](../src/Dashboard.Api/Auth/ApiAuthGuard.cs):
- **Read Endpoints (`GET`)**: Open to allow the public dashboard to display health status, public launcher tiles, print progress, and shared presets.
- **Write Endpoints (`POST`, `DELETE`)**: Protected by `ApiAuthGuard.IsAuthorizedForMutation(req, configuration)`:
  - Accepts a valid `Authorization: Bearer <jwt>` header.
  - In local development (`AllowDevAuthBypass` defaults to `true`), also accepts `X-Dev-Auth: true`.
- **Telemetry Ingestion (`POST /api/print-status`)**:
  - Accepts either standard mutation auth (`Bearer` / `X-Dev-Auth`) or a dedicated `X-Print-Api-Key` header matching `PrintMonitorApiKey` for headless Home Assistant / BamBuddy MQTT bridges.

---

## 4. Client-Side 3D Engine Architecture (`/tools/token-generator`)

The 3D Token Generator mini-app executes 100% in the user's browser to eliminate server compute costs:
- **Blazor UI Layer**: [TokenGenerator.razor](../src/Dashboard.Client/Pages/TokenGenerator.razor) manages parametric state (`TokenPresetDto`) and preset persistence via `DashboardApiClient`.
- **JS Interop Layer**: Uses `.NET 10` `IJSObjectReference` module imports (`./js/tokenGeneratorInterop.js`) to drive:
  - `Three.js` WebGL viewport, lighting, camera, and `OrbitControls`.
  - Parametric cylinder/rim generation, curved polar rim text, and SVG/heightmap face embossing/engraving.
  - Horizontal split-body slicing at `Z = 0` with cylindrical alignment pins and clearance-compensated keyway sockets for support-free FDM printing.
  - Direct binary `.stl` export in the browser via `STLExporter`.

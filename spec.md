# Project Specification: Personal Mini-App Dashboard & 3D Token Generator

## 1. System Architecture & Tech Stack
* **Target Framework:** `.NET 10.0` (LTS) across all client, server, and shared class libraries.
* **Frontend (`src/Dashboard.Client`):** Blazor WebAssembly (Standalone) hosted as a static site on **Netlify** (`DOTNET_VERSION = "10.0"`).
* **Authentication & Routing:** **Auth0** (Google Social Connection) via OIDC (`Microsoft.AspNetCore.Components.WebAssembly.Authentication`).
  * **Public View (`/`)**: Unauthenticated access showcasing public widgets and tools.
  * **Private View (`/private`)**: Protected via `[Authorize]` attribute and custom Auth0 RBAC/email claim validation.
* **Backend Compute (`src/Dashboard.Api`):** **Azure Functions v4** running on the **.NET 10 Isolated Worker** model (Consumption / Flex Consumption Plan) for scale-to-zero cost efficiency.
* **Shared Contracts (`src/Dashboard.Shared`):** `.NET 10` class library containing DTOs, token parameter models, and widget state contracts shared between Blazor and Azure Functions.
* **Database:** **Azure Cosmos DB for NoSQL** (Serverless tier with Free Tier enabled) using `Microsoft.Azure.Cosmos`.
* **Local Development & CI/CD:**
  * VS Code with `.NET 10` Dev Container (`mcr.microsoft.com/devcontainers/dotnet:10.0`).
  * GitHub Actions for Azure Function + Bicep deployments; Netlify Git integration for Blazor WASM publishing.

## 2. Layout & Core Dashboard Widgets
* **Shell Layout:** Responsive CSS Grid dashboard layout with collapsible sidebar navigation (Public vs. Private views) and top bar displaying Auth0 user state.
* **Widgets:**
  1. **Mini-App Quick Launcher:** Configurable tile grid linking to internal tools and external self-hosted endpoints.
  2. **Backend Service Health Monitor:** Polls Azure Function health/status endpoints and displays latency and operational state.
  3. **Bambu Lab A1 Print Monitor:** Displays recent 3D print jobs, current progress, and filament status.
     * *Implementation Note:* Integrate via Home Assistant / BamBuddy MQTT stream pushed to the Azure Function backend or Cosmos DB state store.

## 3. Mini-App 1: In-Browser 3D Token Generator
* **Execution Model:** 100% client-side 3D generation and rendering via Blazor `.NET 10` JS Interop (`IJSObjectReference` wrapping `Three.js`, `STLExporter`, and `three-bvh-csg`).
* **Token Geometry & Features:**
  * **Curved Text:** Parametric text wrapping along the top outer rim and bottom outer rim.
  * **Dual-Sided Embossing/Engraving:** Accepts user-uploaded images/SVGs or heightmaps for both the front and back faces.
  * **Split-Body Print Optimization:** Automatically slices the generated token horizontally along the center plane (`Z = 0`) into two flat-bottomed sections (Front Half and Back Half) with alignment keyways/pins so both halves print flat on the build plate without supports.
* **Output:** Direct client-side browser download of `.stl` files (combined build plate or separate front/back STL files) plus saving preset parameter JSON documents to Cosmos DB.

## 4. Data Model (Cosmos DB Serverless)
* **Database:** `DashboardDb`
* **Container:** `UserItems` (Partition Key: `/partitionKey`)
  * `partitionKey`: `"tokens"` | `"widgets"` | `"print-history"`
  * Stores serialized JSON parameters for saved 3D token presets, custom widget layouts, and cached Bambu A1 print logs.

## 5. Repository Structure
```text
/
├── .devcontainer/
│   └── devcontainer.json       # .NET 10 + Node + Azure Functions v4 Core Tools
├── infra/
│   └── main.bicep              # Azure Functions (.NET 10) + Cosmos DB Serverless
├── src/
│   ├── Dashboard.Client/       # Blazor WASM (net10.0)
│   ├── Dashboard.Api/          # Azure Functions Isolated Worker (net10.0)
│   └── Dashboard.Shared/       # Shared DTOs & Models (net10.0)
├── netlify.toml                # Netlify build config pinned to DOTNET_VERSION = "10.0"
├── MiniAppHub.slnx
└── SPEC.md
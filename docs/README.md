# MiniAppHub (`polyapp`) — Agent & Developer Documentation

This `docs/` directory is the authoritative system reference for AI coding agents and developers working on **MiniAppHub** (`polyapp`).

## Documentation Map

| Document | Purpose | When to Read |
| :--- | :--- | :--- |
| [architecture.md](./architecture.md) | End-to-end system architecture, `.NET 10.0` project boundaries, Auth0 + local dev authentication, and client-side WebGL/Three.js design. | Before modifying project dependencies, authentication, routing, or shell layout. |
| [api-and-data-model.md](./api-and-data-model.md) | Azure Functions HTTP endpoints, request/response DTO contracts, `ApiAuthGuard`, and Cosmos DB `UserItems` partition schema. | Before adding or modifying backend endpoints, shared DTOs, or persistence logic. |
| [adding-mini-apps-and-widgets.md](./adding-mini-apps-and-widgets.md) | Step-by-step extensibility guide for adding new dashboard widgets, `/tools/*` mini-apps, and Blazor `IJSObjectReference` ES modules. | When implementing new dashboard widgets or new client-side mini-apps (e.g., 3D Token Generator). |
| [local-dev-and-ci.md](./local-dev-and-ci.md) | Dev Container configuration, Azurite emulator invariants, VS Code `launch.json`/`tasks.json`, gated GitHub Actions CI (`-warnaserror`), and Netlify hosting. | When running/debugging locally, modifying `.vscode/` or `.github/workflows/`, or troubleshooting builds. |

## Agent Customizations (`.agents/`)

Workspace-scoped rules and skills live in `.agents/` and are automatically discovered by Antigravity:

- **Rules (`.agents/rules/`)**:
  - [AGENTS.md](../.agents/rules/AGENTS.md) — Core invariants for Azurite, VS Code Functions debugging, Linux dev-certs, PR branch synchronization, and `-warnaserror` CI builds.
  - [architecture-and-coding-standards.md](../.agents/rules/architecture-and-coding-standards.md) — `.NET 10.0` project boundaries, Cosmos DB `System.Text.Json` stream serialization, and auth invariants.
- **Skills (`.agents/skills/`)**:
  - [pr-and-ci-workflow](../.agents/skills/pr-and-ci-workflow/SKILL.md) — Syncing `origin/main`, validating `-warnaserror` builds, and creating incremental PRs.
  - [local-fullstack-dev](../.agents/skills/local-fullstack-dev/SKILL.md) — Starting Azurite, `Dashboard.Api` (port `7071`), and `Dashboard.Client` (port `5155`).
  - [scaffold-widget-or-miniapp](../.agents/skills/scaffold-widget-or-miniapp/SKILL.md) — End-to-end checklist for adding new widgets and mini-apps.

---
trigger: always_on
---

# Workspace Agent Rules (MiniAppHub / polyapp)

## System Documentation & Skills Reference

- Before planning or implementing architectural changes, new widgets, or new mini-apps, read the documentation in [docs/README.md](../../docs/README.md):
  - [docs/architecture.md](../../docs/architecture.md) — System architecture, project boundaries, and authentication model.
  - [docs/api-and-data-model.md](../../docs/api-and-data-model.md) — Azure Functions HTTP endpoints, DTO contracts, and Cosmos DB `UserItems` partition schema.
  - [docs/adding-mini-apps-and-widgets.md](../../docs/adding-mini-apps-and-widgets.md) — Playbook for adding new dashboard widgets, mini-app routes, and Blazor JS Interop modules.
  - [docs/local-dev-and-ci.md](../../docs/local-dev-and-ci.md) — Dev container, Azurite, VS Code tasks/launch configuration, and CI/CD pipelines.

## Local Development & Debugging Invariants

1. **Local Storage Emulator (Azurite)**
   - Always use Azurite for local development (`"AzureWebJobsStorage": "UseDevelopmentStorage=true"` in `src/Dashboard.Api/local.settings.json`). Do not blank out `AzureWebJobsStorage` to bypass the emulator check.
   - Keep Azurite data in `/tmp/azurite` (`"azurite.location": "/tmp/azurite"` in `.vscode/settings.json`) to avoid polluting the repository root with `__azurite_db*__` files.
   - When checking or starting Azurite in shell scripts, `.vscode/tasks.json`, or `.devcontainer/devcontainer.json`:
     - Probe port `10000` using `(echo >/dev/tcp/127.0.0.1/10000) >/dev/null 2>&1`. Never use `pgrep -f azurite` (which matches the subshell command string and `code-server --install-extension azurite.azurite`).
     - Launch Azurite in a new session (`setsid azurite --silent --location /tmp/azurite --debug /tmp/azurite/debug.log </dev/null >/dev/null 2>&1 &`) so closing a VS Code task terminal PTY does not terminate the daemon.

2. **Azure Functions & Full-Stack VS Code Launch (`.vscode/launch.json` & `.vscode/tasks.json`)**
   - In `.vscode/launch.json`, the `"Attach to .NET Functions"` configuration (`"processId": "${command:azureFunctions.pickProcess}"`) **must not** include `"preLaunchTask": "func: host start"`. In the VS Code Tasks API, the `type: "func"` task has `name: "host start"` (with `source: "func"`), so setting `preLaunchTask: "func: host start"` causes `ms-azuretools.vscode-azurefunctions` to fail with `Failed to find "func: host start" task.`. Omitting `preLaunchTask` lets `azureFunctions.pickProcess` find and start the `func` task automatically.
   - Do not make `build (functions)` depend on `clean (functions)`. Cleaning before every build deletes `Dashboard.Shared.dll` (causing parallel MSBuild lock collisions with `Dashboard.Client`) and forces a slow NuGet re-restore of `WorkerExtensions`.
   - Ensure `src/Dashboard.Api/Dashboard.Api.csproj` preserves `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>` for `host.json` and `local.settings.json`.

3. **.NET HTTPS Development Certificates on Linux (`.devcontainer/devcontainer.json`)**
   - On Linux (.NET 9/10), `dotnet dev-certs https --trust` requires `SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs"`. Always preserve `SSL_CERT_DIR` in `containerEnv` and any certificate-trust task scripts so `dotnet dev-certs https --trust` exits with code `0` instead of code `4` (`PartialTrust`).

## Pull Request & Gated CI Build Invariants

4. **Incremental PR Workflow & Branch Synchronization**
   - Implement multi-part features as separate, independently verifiable Pull Requests rather than a single monolithic change.
   - Before starting a new PR after the previous PR is merged on GitHub, always fetch the latest `origin/main` (`git fetch origin main`), fast-forward local `main` (`git branch -f main origin/main`), and create the new feature branch directly from `origin/main` (`git checkout -B <branch-name> origin/main`).

5. **Gated GitHub CI Build (`-warnaserror`)**
   - The `main` branch enforces the `build` status check defined in `.github/workflows/ci-build.yml` (`strict: true`, `enforce_admins: true`). Do not bypass branch protection or modify `.github/workflows/ci-build.yml` to weaken compiler checks.
   - Before pushing or opening a PR, always verify that `dotnet build MiniAppHub.slnx --configuration Release -warnaserror` succeeds locally with `0 Warning(s)` and `0 Error(s)`.

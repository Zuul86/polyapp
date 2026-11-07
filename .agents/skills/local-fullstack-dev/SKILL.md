---
name: local-fullstack-dev
description: >-
  Use this skill when starting, debugging, or testing the MiniAppHub full-stack
  application locally (Azurite storage emulator, Azure Functions API on port
  7071, Blazor WebAssembly client on port 5155, or Linux .NET HTTPS dev-certs).
---

# Local Full-Stack Development & Debugging

Use this runbook to start and verify the full-stack environment (`Azurite` + `Dashboard.Api` + `Dashboard.Client`) inside the `.NET 10` Dev Container.

## 1. Start or Verify Azurite Storage Emulator

`Dashboard.Api` requires Azurite (`"AzureWebJobsStorage": "UseDevelopmentStorage=true"` in [local.settings.json](../../../src/Dashboard.Api/local.settings.json)).

1. Check if Azurite Blob service is listening on `127.0.0.1:10000`:
   ```bash
   (echo >/dev/tcp/127.0.0.1/10000) >/dev/null 2>&1 && echo "Azurite running" || echo "Azurite stopped"
   ```
2. If stopped, start Azurite in a detached session storing state in `/tmp/azurite` (never in the workspace root):
   ```bash
   mkdir -p /tmp/azurite
   setsid azurite --silent --location /tmp/azurite --debug /tmp/azurite/debug.log </dev/null >/dev/null 2>&1 &
   ```

## 2. Trust .NET HTTPS Development Certificate (Linux)

On Linux containers with `.NET 10`, always ensure `SSL_CERT_DIR` includes `$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs`:

```bash
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs"
dotnet dev-certs https --check --trust || dotnet dev-certs https --trust
```

## 3. Run the Azure Functions API (`Dashboard.Api`)

Start the isolated worker Functions host on `http://localhost:7071`:

```bash
func start --script-root /workspaces/polyapp/src/Dashboard.Api --port 7071
```

Verify endpoints once running:
- Health check: `curl -sS http://localhost:7071/api/health`
- Widgets list: `curl -sS http://localhost:7071/api/items/widgets`
- Print status feed: `curl -sS http://localhost:7071/api/print-status`

## 4. Run the Blazor WASM Client (`Dashboard.Client`)

Start the Blazor WebAssembly client on `http://localhost:5155`:

```bash
dotnet run --project /workspaces/polyapp/src/Dashboard.Client/Dashboard.Client.csproj --launch-profile http
```

## 5. VS Code F5 Full-Stack Debugging Notes

- Use the **"Full Stack (Client + Functions API)"** compound launch profile in [.vscode/launch.json](../../../.vscode/launch.json).
- Do **not** add `"preLaunchTask": "func: host start"` to `"Attach to .NET Functions"`; `${command:azureFunctions.pickProcess}` automatically resolves and starts the `type: "func"` task in [.vscode/tasks.json](../../../.vscode/tasks.json).

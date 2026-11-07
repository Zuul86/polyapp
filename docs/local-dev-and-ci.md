# Local Development, VS Code Debugging & CI/CD

## 1. Dev Container Environment (`.devcontainer/`)

The workspace runs inside a `.NET 10.0` Linux Dev Container defined in [.devcontainer/devcontainer.json](../.devcontainer/devcontainer.json):
- **Base Image:** `mcr.microsoft.com/devcontainers/dotnet:10.0`
- **Features:** Node.js LTS (`22`), Azure CLI (`az` + Bicep)
- **Global Tools:** Azure Functions Core Tools v4 (`azure-functions-core-tools@4`) and Azurite (`azurite`)
- **Ports:**
  - `5155`: `Dashboard.Client` (Blazor WebAssembly HTTP profile)
  - `7071`: `Dashboard.Api` (Azure Functions v4 HTTP host)
  - `10000–10002`: Azurite Blob, Queue, and Table emulators

### Linux HTTPS Dev-Certs (`SSL_CERT_DIR`)
On Linux with `.NET 9/10`, `dotnet dev-certs https --trust` writes user certificates to `$HOME/.aspnet/dev-certs/trust` and requires OpenSSL's `SSL_CERT_DIR` environment variable to include both that directory and `/usr/lib/ssl/certs`:
```bash
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs"
dotnet dev-certs https --trust
```
Both [.devcontainer/devcontainer.json](../.devcontainer/devcontainer.json) (`containerEnv`) and the `trust (dev-certs)` task in [.vscode/tasks.json](../.vscode/tasks.json) preserve this variable so certificate checks exit with code `0`.

---

## 2. Azurite Storage Emulator Invariants

`src/Dashboard.Api/local.settings.json` configures `"AzureWebJobsStorage": "UseDevelopmentStorage=true"`.
- **Storage Location:** Always run Azurite with `--location /tmp/azurite` (configured in [.vscode/settings.json](../.vscode/settings.json) and [.vscode/tasks.json](../.vscode/tasks.json)) so `__azurite_db*__` files are never created in the git repository root.
- **Health Probe:** Check whether Azurite is running by probing TCP port `10000`:
  ```bash
  (echo >/dev/tcp/127.0.0.1/10000) >/dev/null 2>&1
  ```
  Never use `pgrep -f azurite`, which false-matches subshell commands and VS Code extension installers.
- **Detached Session:** Always start Azurite with `setsid ... </dev/null >/dev/null 2>&1 &` so closing a task terminal does not send `SIGHUP` to the Azurite daemon.

---

## 3. VS Code Debugging (`.vscode/launch.json` & `.vscode/tasks.json`)

### Launch Profiles ([.vscode/launch.json](../.vscode/launch.json))
1. **`Launch Dashboard.Client (HTTP)` / `(HTTPS)`**: Builds `Dashboard.Client` (`preLaunchTask: "build (client)"`) and runs `dotnet run --no-build`.
2. **`Attach to .NET Functions`**: Uses `"processId": "${command:azureFunctions.pickProcess}"`.
   - **Important Invariant:** Do **not** add `"preLaunchTask": "func: host start"` to `"Attach to .NET Functions"`. The VS Code Azure Functions extension (`ms-azuretools.vscode-azurefunctions`) registers the `type: "func"` task with `name: "host start"` and `source: "func"`, and `azureFunctions.pickProcess` automatically finds and starts it.
3. **`Full Stack (Client + Functions API)`**: Compound launch configuration that runs `preLaunchTask: "build (functions)"` (which depends on `start (azurite)`) and launches both the Functions debugger and the Blazor WASM client.

### Build Task Graph ([.vscode/tasks.json](../.vscode/tasks.json))
- `build (functions)` depends on `start (azurite)` (and **never** on `clean (functions)`, avoiding parallel MSBuild lock collisions on `Dashboard.Shared.dll` and unnecessary `WorkerExtensions` NuGet restores).
- `build (client)` depends on `trust (dev-certs)`.

---

## 4. Gated GitHub Actions CI & Branch Protection

Defined in [.github/workflows/ci-build.yml](../.github/workflows/ci-build.yml):
- Triggers on every `pull_request` targeting `main` and every `push` to `main`.
- Runs on `ubuntu-latest` with `.NET 10.0.x`:
  1. `dotnet restore MiniAppHub.slnx`
  2. `dotnet build MiniAppHub.slnx --configuration Release --no-restore -warnaserror`
  3. `dotnet publish src/Dashboard.Client/Dashboard.Client.csproj --configuration Release --no-build --output release`
- **Branch Protection:** `main` requires the `build` check to pass (`strict: true`, `enforce_admins: true`) before any PR can be merged.

---

## 5. Static Frontend Hosting (Netlify)

Configured in [netlify.toml](../netlify.toml):
- Pins `DOTNET_VERSION = "10.0"`.
- Publishes `src/Dashboard.Client/Dashboard.Client.csproj -c Release -o release` and serves `release/wwwroot`.
- Includes a SPA fallback redirect (`/* -> /index.html`, status `200`) so client-side Blazor routes (`/private`, `/tools/token-generator`, `/authentication/*`) resolve properly on direct navigation or page refresh.

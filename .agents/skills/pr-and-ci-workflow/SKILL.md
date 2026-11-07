---
name: pr-and-ci-workflow
description: >-
  Use this skill when starting a new feature branch, preparing a Pull Request,
  validating the gated CI build (-warnaserror), or syncing with origin/main
  after a PR is merged in the MiniAppHub (polyapp) repository.
---

# Pull Request & Gated CI Workflow

Follow this workflow whenever creating, updating, or transitioning between Pull Requests in `polyapp`.

## 1. Starting a New PR Branch (After Previous PR Merge)

Always synchronize with `origin/main` before writing code for the next PR:

1. Fetch the latest `main` branch from GitHub:
   ```bash
   git fetch origin main
   ```
2. Fast-forward the local `main` branch pointer:
   ```bash
   git branch -f main origin/main
   ```
3. Create and switch to the new feature branch directly from `origin/main`:
   ```bash
   git checkout -B pr-<number>-<short-slug> origin/main
   ```
4. Verify a clean working tree:
   ```bash
   git status -s
   ```

## 2. Pre-Push Gated CI Validation

The `main` branch enforces the `CI Build` (`build`) status check defined in [.github/workflows/ci-build.yml](../../../.github/workflows/ci-build.yml). Every PR must pass these exact commands with `0 Warning(s)` and `0 Error(s)` before pushing:

1. Restore packages across the solution:
   ```bash
   dotnet restore MiniAppHub.slnx
   ```
2. Build in `Release` configuration with warnings treated as errors:
   ```bash
   dotnet build MiniAppHub.slnx --configuration Release --no-restore -warnaserror
   ```
3. Verify that the standalone Blazor WASM client publishes cleanly:
   ```bash
   dotnet publish src/Dashboard.Client/Dashboard.Client.csproj --configuration Release --no-build --output /tmp/polyapp-release-check
   ```

## 3. Pushing & Opening the Pull Request

1. Stage and commit changes with a descriptive message:
   ```bash
   git add -A
   git commit -m "PR <N>: <Concise summary of feature>"
   ```
2. Push the branch to `origin`:
   ```bash
   git push -u origin pr-<number>-<short-slug>
   ```
3. Create the PR on GitHub targeting `main`:
   ```bash
   gh pr create --base main --head pr-<number>-<short-slug> --title "PR <N>: <Title>" --body "<Summary and verification steps>"
   ```
4. Check the GitHub Actions CI status on the PR:
   ```bash
   gh pr checks pr-<number>-<short-slug>
   ```

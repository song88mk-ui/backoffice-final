---
description: Create and push the next incremental staging release tag (v0.1.X-stg) to trigger a STG deployment.
allowed-tools: Bash(git fetch:*), PowerShell(.\scripts\New-ReleaseTag.ps1 -Environment stg -DryRun)
---

# /push-stg

Create and push the next incremental **staging** release tag to trigger a deployment to STG.

Tags follow the pattern `vMAJOR.MINOR.PATCH-stg` (e.g. `v0.1.52-stg`). The next tag is the latest `-stg` tag with its PATCH number incremented by one.

## Steps

1. **Confirm intent first.** Show the user the exact tag that will be created using a dry run, and ask for explicit confirmation:

   ```powershell
   .\scripts\New-ReleaseTag.ps1 -Environment stg -DryRun
   ```

2. Only after the user explicitly confirms, create the release:

   ```powershell
   .\scripts\New-ReleaseTag.ps1 -Environment stg
   ```

   The script already does the following, so do not duplicate it manually:
   - `git fetch --tags --prune --force origin` (so the newest remote tag is seen)
   - finds the highest existing `-stg` tag (numeric sort, not lexical)
   - increments the PATCH by 1
   - creates a **GitHub Release** for the new tag (target `main`), which also creates the tag and triggers the CI deploy

3. Report back the exact tag/release that was created, and confirm that the STG deployment has been triggered.

## Notes

- **Never create a `-stg` release without explicit user confirmation in the conversation.** STG is shared by the whole team and the QA suites, and a `-stg` release builds and pushes all six BO images (the `ci.yml` build matrix) to the STG account.
- The deploy is triggered by a **published GitHub Release** (`release: released` in `.github/workflows/ci.yml`), **not** by a tag push. The script creates the Release for this reason; a bare tag will not deploy.
- The script needs either GitHub CLI (`gh auth login`) or a `GH_TOKEN`/`GITHUB_TOKEN` env var (contents:write / repo scope) to create the Release. If neither is present it stops with instructions instead of leaving a tag with no release.
- If the script reports the tag/release already exists or no prior `-stg` tag is found, surface the error instead of retrying blindly.
- This creates a release/tag on `origin` only — it does not push code commits (push `main` first).

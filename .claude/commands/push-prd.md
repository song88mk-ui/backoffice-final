---
description: Create and push the next incremental production release tag (v0.1.X-prd) to trigger a PRD deployment.
allowed-tools: Bash(git fetch:*), PowerShell(.\scripts\New-ReleaseTag.ps1 -Environment prd -DryRun)
---

# /push-prd

Create and push the next incremental **production** release tag to trigger a deployment to PRD.

Tags follow the pattern `vMAJOR.MINOR.PATCH-prd` (e.g. `v0.1.40-prd`). The next tag is the latest `-prd` tag with its PATCH number incremented by one.

## Steps

1. **Confirm intent first.** Production is live infrastructure processing real financial transactions. Before pushing, show the user the exact tag that will be created using a dry run and ask for explicit confirmation:

   ```powershell
   .\scripts\New-ReleaseTag.ps1 -Environment prd -DryRun
   ```

2. Only after the user explicitly confirms, create and push the tag:

   ```powershell
   .\scripts\New-ReleaseTag.ps1 -Environment prd
   ```

   The script already does the following, so do not duplicate it manually:
   - `git fetch --tags --prune --force origin` (so the newest remote tag is seen)
   - finds the highest existing `-prd` tag (numeric sort, not lexical)
   - increments the PATCH by 1
   - creates a **GitHub Release** for the new tag (target `main`), which also creates the tag and triggers the CI deploy

3. Report back the exact tag/release that was created, and confirm that the PRD deployment has been triggered.

## Notes

- **Never create a `-prd` release without explicit user confirmation in the conversation.** Publishing the Release is what deploys to production (`release: released` in `.github/workflows/ci.yml`) — a bare tag does not.
- The script needs either GitHub CLI (`gh auth login`) or a `GH_TOKEN`/`GITHUB_TOKEN` env var (contents:write / repo scope) to create the Release.
- If the script reports the tag/release already exists or no prior `-prd` tag is found, surface the error instead of retrying blindly.
- This creates a release/tag on `origin` only — it does not push code commits (push `main` first).

#requires -Version 5.1
<#
.SYNOPSIS
    Creates an incremental GitHub Release to trigger a deployment.

.DESCRIPTION
    Tags follow the pattern  vMAJOR.MINOR.PATCH-<env>  (e.g. v0.1.52-stg).
    The script finds the highest existing tag for the chosen environment,
    increments the PATCH number by one, then creates a GitHub *Release* for the
    new tag (targeting main).

    IMPORTANT: the CI/deploy workflow (.github/workflows/ci.yml) triggers on a
    published Release (`release: released`), NOT on a tag push. A plain git tag
    will not deploy — a Release must be created. This script creates the Release
    (which also creates the tag) so the deployment is actually triggered.

    Requires one of:
      - GitHub CLI installed + authenticated  (gh auth login), or
      - a token in $env:GH_TOKEN / $env:GITHUB_TOKEN (contents:write / repo scope).
    The Release is created as a full release (prerelease = false) so the
    `released` event fires and CI runs.

.PARAMETER Environment
    Target environment suffix. 'stg' (staging) or 'prd' (production).

.PARAMETER DryRun
    Show the tag that would be created without creating or pushing it.

.EXAMPLE
    .\scripts\New-ReleaseTag.ps1 -Environment stg
    # If latest is v0.1.50-stg -> creates and pushes v0.1.51-stg

.EXAMPLE
    .\scripts\New-ReleaseTag.ps1 -Environment prd -DryRun
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('stg', 'prd')]
    [string]$Environment,

    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

# Make sure local tags match the remote so we don't reuse a number.
Write-Host "Fetching latest tags from origin..." -ForegroundColor Cyan
git fetch --tags --prune --force origin | Out-Null

$suffix  = "-$Environment"
$pattern = '^v(\d+)\.(\d+)\.(\d+)' + [regex]::Escape($suffix) + '$'

# Collect all matching tags and pick the highest by numeric MAJOR.MINOR.PATCH.
$latest = git tag |
    ForEach-Object {
        if ($_ -match $pattern) {
            [pscustomobject]@{
                Tag   = $_
                Major = [int]$Matches[1]
                Minor = [int]$Matches[2]
                Patch = [int]$Matches[3]
            }
        }
    } |
    Sort-Object Major, Minor, Patch |
    Select-Object -Last 1

if (-not $latest) {
    throw "No existing '$suffix' tags found. Create the first one manually (e.g. v0.1.1$suffix)."
}

$newTag = "v{0}.{1}.{2}{3}" -f $latest.Major, $latest.Minor, ($latest.Patch + 1), $suffix

Write-Host "Latest $Environment tag : $($latest.Tag)" -ForegroundColor Green
Write-Host "Next tag to create    : $newTag" -ForegroundColor Yellow

if ($DryRun) {
    Write-Host "[DryRun] No tag or release created." -ForegroundColor DarkYellow
    return
}

# Safety: don't reuse an existing tag/release number.
if (git tag --list $newTag) {
    throw "Tag $newTag already exists locally. Aborting."
}

# ---------------------------------------------------------------------------
# Create the GitHub *Release* (not just a tag). The CI/deploy workflow triggers
# on a published Release (`release: released`), NOT on a tag push, so a plain tag
# would never deploy. Creating the Release also creates the tag on `main`.
# It must be a full release (prerelease = false) so the `released` event fires.
# ---------------------------------------------------------------------------

$originUrl = (git remote get-url origin).Trim()
if ($originUrl -notmatch 'github\.com[/:]([^/]+)/(.+?)(?:\.git)?$') {
    throw "Could not parse GitHub owner/repo from origin URL: $originUrl"
}
$owner = $Matches[1]
$repo  = $Matches[2]

$gh = Get-Command gh -ErrorAction SilentlyContinue
if ($gh) {
    Write-Host "Creating GitHub release $newTag via gh CLI..." -ForegroundColor Cyan
    gh release create $newTag --target main --title $newTag --generate-notes
    if ($LASTEXITCODE -ne 0) { throw "gh release create failed (exit $LASTEXITCODE). No release created for $newTag." }
}
else {
    $token = $env:GH_TOKEN
    if (-not $token) { $token = $env:GITHUB_TOKEN }
    if (-not $token) {
        throw @"
Cannot create the GitHub Release for ${newTag}: GitHub CLI (gh) is not installed and
neither GH_TOKEN nor GITHUB_TOKEN is set.

The deploy workflow triggers on a *published Release* (release: released), not on a
tag push, so NO deployment happens until a Release exists.

Enable one of the following, then re-run:
  - Install GitHub CLI:   winget install GitHub.cli   (then: gh auth login)
  - Or set a token:       `$env:GH_TOKEN = '<PAT with contents:write / repo scope>'

Or create the Release for $newTag manually in the GitHub UI (Releases -> Draft a new release).
"@
    }

    Write-Host "Creating GitHub release $newTag via REST API..." -ForegroundColor Cyan
    $headers = @{
        Authorization          = "Bearer $token"
        "User-Agent"           = "New-ReleaseTag.ps1"
        Accept                 = "application/vnd.github+json"
        "X-GitHub-Api-Version" = "2022-11-28"
    }
    $body = @{
        tag_name               = $newTag
        target_commitish       = "main"
        name                   = $newTag
        draft                  = $false
        prerelease             = $false
        generate_release_notes = $true
    } | ConvertTo-Json

    try {
        Invoke-RestMethod -Method Post -Uri "https://api.github.com/repos/$owner/$repo/releases" -Headers $headers -Body $body | Out-Null
    }
    catch {
        throw "Failed to create GitHub Release for $newTag via REST API: $($_.Exception.Message)"
    }
}

Write-Host "Created GitHub Release $newTag (target: main) -> $Environment deployment triggered." -ForegroundColor Green

#requires -Version 5.1
<#
.SYNOPSIS
    Creates and pushes an incremental release tag to trigger a deployment.

.DESCRIPTION
    Tags follow the pattern  vMAJOR.MINOR.PATCH-<env>  (e.g. v0.1.52-stg).
    The script finds the highest existing tag for the chosen environment,
    increments the PATCH number by one, then creates and pushes the new tag.

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
    Write-Host "[DryRun] No tag created or pushed." -ForegroundColor DarkYellow
    return
}

# Safety: don't clobber an existing tag.
if (git tag --list $newTag) {
    throw "Tag $newTag already exists locally. Aborting."
}

git tag $newTag
git push origin $newTag

Write-Host "Created and pushed $newTag -> deployment to $Environment triggered." -ForegroundColor Green

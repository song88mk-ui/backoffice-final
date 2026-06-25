<#
.SYNOPSIS
    Backoffice API Health Check Script

.DESCRIPTION
    Validates connectivity and basic responses for all external APIs consumed by the
    Backoffice solution (Platform API, Trading API). Reads configuration directly from
    Backoffice.API/appsettings.json so it always reflects the active environment.

    Run this script before investigating Backoffice.Web errors to rule out upstream
    API failures as the root cause.

.EXAMPLE
    .\scripts\Test-BackofficeAPIs.ps1
    .\scripts\Test-BackofficeAPIs.ps1 -Region SA
    .\scripts\Test-BackofficeAPIs.ps1 -Region HK -BetId "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"

.PARAMETER Region
    Which region config to use: HK (default) or SA.

.PARAMETER BetId
    Optional. A real BetId GUID to use when testing the Market Settlement endpoint.
    When omitted, a zero GUID is used (validates reachability, not data).

.PARAMETER AppsettingsPath
    Path to appsettings.json. Defaults to Backoffice.API\appsettings.json relative
    to the script location.
#>
param(
    [ValidateSet("HK", "SA")]
    [string]$Region = "HK",

    [string]$BetId = "00000000-0000-0000-0000-000000000001",

    [string]$AppsettingsPath = ""
)

# ---------------------------------------------------------------------------
# Config
# ---------------------------------------------------------------------------

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $AppsettingsPath) {
    # Script lives in scripts/; appsettings.json is one level up in the project folder.
    $AppsettingsPath = Join-Path $scriptDir "..\Backoffice.API\appsettings.json"
}

if (-not (Test-Path $AppsettingsPath)) {
    Write-Host "[ERROR] appsettings.json not found at: $AppsettingsPath" -ForegroundColor Red
    exit 1
}

# Strip // line comments — not valid JSON but used in appsettings.json
$rawJson = (Get-Content $AppsettingsPath -Raw) -replace '(?m)^\s*//.*$', ''
$config  = $rawJson | ConvertFrom-Json

$platformUri = $config.API.PlatformAPI.$Region.Uri.TrimEnd("/")
$platformKey  = $config.API.PlatformAPI.$Region.APIKey
# Trading API removed 2026-06 — $tradingUri / $tradingKey no longer used

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Invoke-Check {
    param(
        [string]$Group,
        [string]$Label,
        [string]$Method,
        [string]$Url,
        [hashtable]$Headers,
        [string]$Body = $null,
        [int[]]$AcceptedCodes = @(200, 400, 404)
    )

    $sw = [System.Diagnostics.Stopwatch]::StartNew()

    try {
        $params = @{
            Uri             = $Url
            Method          = $Method
            Headers         = $Headers
            TimeoutSec      = 15
            UseBasicParsing = $true
            ErrorAction     = "Stop"
        }
        if ($Body) {
            $params["Body"]        = $Body
            $params["ContentType"] = "application/json"
        }

        $resp = Invoke-WebRequest @params
        $sw.Stop()
        $code = [int]$resp.StatusCode
        $ok   = $AcceptedCodes -contains $code

        return [PSCustomObject]@{
            Group  = $Group
            Label  = $Label
            Status = if ($ok) { "OK" } else { "WARN" }
            Code   = "$code"
            Ms     = $sw.ElapsedMilliseconds
            Detail = ""
        }
    }
    catch {
        $sw.Stop()
        $code = 0
        try { $code = [int]$_.Exception.Response.StatusCode } catch {}

        $isDown = ($code -eq 0 -or $code -eq 503 -or $code -eq 502)
        $msg    = $_.Exception.Message.Split("`n")[0]

        return [PSCustomObject]@{
            Group  = $Group
            Label  = $Label
            Status = if ($isDown) { "DOWN" } else { "WARN" }
            Code   = if ($code -eq 0) { "ERR" } else { "$code" }
            Ms     = $sw.ElapsedMilliseconds
            Detail = $msg
        }
    }
}

function Write-Header([string]$text) {
    Write-Host ""
    Write-Host ("=" * 65) -ForegroundColor Cyan
    Write-Host "  $text" -ForegroundColor Cyan
    Write-Host ("=" * 65) -ForegroundColor Cyan
}

function Write-Section([string]$text) {
    Write-Host ""
    Write-Host "  -- $text --" -ForegroundColor DarkCyan
}

# ---------------------------------------------------------------------------
# Print environment summary
# ---------------------------------------------------------------------------

Write-Header "Backoffice API Health Check  [Region: $Region]"
Write-Host ""
Write-Host "  Config file   : $AppsettingsPath"
Write-Host "  Platform API  : $platformUri"
Write-Host "  Trading API   : removed 2026-06 (all endpoints return 410)"
Write-Host "  Test BetId    : $BetId"
Write-Host ""

# ---------------------------------------------------------------------------
# Run checks
# ---------------------------------------------------------------------------

$results = [System.Collections.ArrayList]::new()

$ph = @{ "x-api-key" = $platformKey }
$th = @{ "x-api-key" = $tradingKey  }

Write-Section "Platform API - Connectivity"
$null = $results.Add((Invoke-Check -Group "Platform" -Label "Sports lookup"             -Method GET  -Url "$platformUri/v1/client/sports"                      -Headers $ph))
$null = $results.Add((Invoke-Check -Group "Platform" -Label "Exchange rates"            -Method GET  -Url "$platformUri/v1/client/exchangerates"               -Headers $ph))
$null = $results.Add((Invoke-Check -Group "Platform" -Label "Clients list"              -Method GET  -Url "$platformUri/v1/clients"                            -Headers $ph))
$null = $results.Add((Invoke-Check -Group "Platform" -Label "Sports tree"               -Method GET  -Url "$platformUri/v1/client/topfixtures/sportstree"       -Headers $ph))

Write-Section "Platform API - Market Settlement"
$betBody = "{`"BetIds`":[`"$BetId`"]}"
$null = $results.Add((Invoke-Check -Group "Platform/Settlement" -Label "Bet details"               -Method POST -Url "$platformUri/v1/bet/details"                   -Headers $ph -Body $betBody))
$null = $results.Add((Invoke-Check -Group "Platform/Settlement" -Label "Resend settlement details"  -Method POST -Url "$platformUri/v1/bet/resendsettlement/details"  -Headers $ph -Body $betBody))

# Trading API removed 2026-06 — all api/trading/* endpoints now return 410 Gone

# ---------------------------------------------------------------------------
# Print results table
# ---------------------------------------------------------------------------

Write-Host ""
Write-Host ("=" * 65) -ForegroundColor Cyan
Write-Host "  Results" -ForegroundColor Cyan
Write-Host ("=" * 65) -ForegroundColor Cyan
Write-Host ""
Write-Host ("  " + "Status".PadRight(6) + " " + "Code".PadRight(5) + " " + "Ms".PadLeft(6) + "  Endpoint") -ForegroundColor Gray
Write-Host ("  " + ("-" * 60)) -ForegroundColor Gray

$prevGroup = ""
foreach ($r in $results) {
    if ($r.Group -ne $prevGroup) {
        Write-Host ""
        Write-Host "  [$($r.Group)]" -ForegroundColor DarkCyan
        $prevGroup = $r.Group
    }

    $color = switch ($r.Status) {
        "OK"   { "Green"  }
        "WARN" { "Yellow" }
        "DOWN" { "Red"    }
        default{ "Gray"   }
    }

    Write-Host ("  " + $r.Status.PadRight(6) + " " + $r.Code.PadRight(5) + " " + "$($r.Ms)".PadLeft(6) + "ms  " + $r.Label) -ForegroundColor $color

    if ($r.Detail) {
        $trimmed = if ($r.Detail.Length -gt 100) { $r.Detail.Substring(0, 100) + "..." } else { $r.Detail }
        Write-Host ("    $trimmed") -ForegroundColor DarkGray
    }
}

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

$down  = @($results | Where-Object { $_.Status -eq "DOWN" }).Count
$warn  = @($results | Where-Object { $_.Status -eq "WARN" }).Count
$ok    = @($results | Where-Object { $_.Status -eq "OK"   }).Count
$total = $results.Count

Write-Host ""
Write-Host ("=" * 65) -ForegroundColor Cyan
$summaryColor = if ($down -gt 0) { "Red" } elseif ($warn -gt 0) { "Yellow" } else { "Green" }
Write-Host ("  Total: $total   OK: $ok   WARN: $warn   DOWN: $down") -ForegroundColor $summaryColor
Write-Host ("=" * 65) -ForegroundColor Cyan
Write-Host ""

if ($down -gt 0) { exit 2 }
if ($warn -gt 0) { exit 1 }
exit 0

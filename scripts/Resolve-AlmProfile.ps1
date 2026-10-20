param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Test-AlmPlaceholderValue {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $true
    }

    return (
        $Value -match '(^|[/=:])replace-with-' -or
        $Value -match '(^|[/=:])replace-after-' -or
        $Value -match '^00000000-0000-0000-0000-000000000000$' -or
        $Value -match '^planner-plan-id$' -or
        $Value -match '^https?://contoso\.sharepoint\.com(/|$)'
    )
}

function Get-AlmProfiles {
    param(
        [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
        [string]$ProfilesPath = 'powerplatform/settings/alm-profiles.json'
    )

    $fullPath = if ([System.IO.Path]::IsPathRooted($ProfilesPath)) {
        $ProfilesPath
    } else {
        Join-Path $RepoRoot $ProfilesPath
    }

    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw "ALM profile catalog not found: $fullPath"
    }

    $profiles = Get-Content -LiteralPath $fullPath -Raw | ConvertFrom-Json
    if (-not $profiles.profiles) {
        throw "ALM profile catalog does not contain any profiles: $fullPath"
    }

    return [pscustomobject]@{
        Path = $fullPath
        Profiles = @($profiles.profiles)
    }
}

function Resolve-AlmProfile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProfileName,
        [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
        [string]$ProfilesPath = 'powerplatform/settings/alm-profiles.json'
    )

    $catalog = Get-AlmProfiles -RepoRoot $RepoRoot -ProfilesPath $ProfilesPath
    $profile = @($catalog.Profiles | Where-Object { $_.name -eq $ProfileName } | Select-Object -First 1)
    if (-not $profile) {
        $known = @($catalog.Profiles | ForEach-Object { $_.name }) -join ', '
        throw "Unknown ALM profile '$ProfileName'. Known profiles: $known"
    }

    return [pscustomobject]@{
        CatalogPath = $catalog.Path
        Profile = $profile
    }
}

function Resolve-AlmTargetEnvironmentUrl {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Profile,
        [string]$EnvironmentUrl = $env:POWER_PLATFORM_ENVIRONMENT_URL
    )

    if (-not [string]::IsNullOrWhiteSpace($EnvironmentUrl) -and -not (Test-AlmPlaceholderValue -Value $EnvironmentUrl)) {
        return $EnvironmentUrl
    }

    if ($Profile.environmentUrl -and -not (Test-AlmPlaceholderValue -Value ([string]$Profile.environmentUrl))) {
        return [string]$Profile.environmentUrl
    }

    throw "A concrete environment URL is required for profile '$($Profile.name)'. Provide -EnvironmentUrl or set POWER_PLATFORM_ENVIRONMENT_URL."
}

function Get-PacAuthEntries {
    if (-not (Get-Command pac -ErrorAction SilentlyContinue)) {
        throw "Power Platform CLI 'pac' was not found on PATH."
    }

    $output = & pac auth list 2>&1
    $exitCode = $LASTEXITCODE
    $text = ($output | Out-String).Trim()

    if ($exitCode -ne 0) {
        throw "pac auth list failed: $text"
    }

    $entries = New-Object System.Collections.Generic.List[object]
    foreach ($match in [regex]::Matches($text, '(?ms)^\[(?<Index>\d+)\](?<Block>.*?)(?=^\[\d+\]|\z)')) {
        $block = $match.Groups['Block'].Value
        $urlMatch = [regex]::Match($block, 'https?://\S+')
        if (-not $urlMatch.Success) {
            continue
        }

        $entries.Add([pscustomobject]@{
            Index = [int]$match.Groups['Index'].Value
            Active = ($block -match '\*\s')
            EnvironmentUrl = $urlMatch.Value.Trim()
            Raw = ('[' + $match.Groups['Index'].Value + ']' + $block).Trim()
        })
    }

    return [pscustomobject]@{
        RawOutput = $text
        Entries = @($entries.ToArray())
    }
}

function Select-PacAuthEnvironment {
    param(
        [Parameter(Mandatory = $true)]
        [string]$EnvironmentUrl
    )

    $catalog = Get-PacAuthEntries
    $entry = $catalog.Entries | Where-Object { $_.EnvironmentUrl -eq $EnvironmentUrl } | Select-Object -First 1
    if (-not $entry) {
        throw "No PAC auth entry matched environment URL '$EnvironmentUrl'. Current auth list:`n$($catalog.RawOutput)"
    }

    if (-not $entry.Active) {
        & pac auth select --index $entry.Index
        if ($LASTEXITCODE -ne 0) {
            throw "pac auth select failed for index $($entry.Index) targeting '$EnvironmentUrl'."
        }
    }

    return $entry
}

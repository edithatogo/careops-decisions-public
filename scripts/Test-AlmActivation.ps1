param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

function Assert-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing ALM activation artifact: $RelativePath"
    }

    $json = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    Write-Host "Valid ALM JSON: $RelativePath"
    return $json
}

function Assert-FilePresent {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing ALM activation artifact: $RelativePath"
    }

    $content = Get-Content -LiteralPath $path -Raw
    if ([string]::IsNullOrWhiteSpace($content)) {
        throw "ALM activation artifact is empty: $RelativePath"
    }

    Write-Host "Present ALM artifact: $RelativePath"
}

$profiles = Assert-JsonFile -RelativePath 'powerplatform/settings/alm-profiles.json'
$connections = Assert-JsonFile -RelativePath 'powerplatform/settings/connection-references.json'
$envVars = Assert-JsonFile -RelativePath 'powerplatform/settings/environment-variable-manifest.json'

Assert-FilePresent -RelativePath '.github/workflows/validate.yml'
Assert-FilePresent -RelativePath 'docs/deployment/alm-activation.md'
Assert-FilePresent -RelativePath 'scripts/Invoke-SolutionChecker.ps1'

$profileNames = @($profiles.profiles | ForEach-Object { $_.name })
foreach ($requiredProfile in @('local', 'development', 'test', 'enterprise')) {
    if ($profileNames -notcontains $requiredProfile) {
        throw "Missing ALM profile: $requiredProfile"
    }
}

$profileLookup = @{}
foreach ($profile in @($profiles.profiles)) {
    $profileLookup[$profile.name] = $profile
}

if (-not $profileLookup.ContainsKey('local') -or [bool]$profileLookup.local.allowLiveWrites) {
    throw "ALM profile 'local' must not allow live writes."
}

if (-not $profileLookup.ContainsKey('development') -or [bool]$profileLookup.development.allowLiveWrites) {
    throw "Public source ALM profile 'development' must not allow live writes."
}

foreach ($nonLiveProfile in @('test', 'enterprise')) {
    if ($profileLookup.ContainsKey($nonLiveProfile) -and [bool]$profileLookup[$nonLiveProfile].allowLiveWrites) {
        throw "ALM profile '$nonLiveProfile' must not allow live writes."
    }
}

if ($profileLookup.local.environmentUrl) {
    throw "ALM profile 'local' must not carry a live environmentUrl."
}

if ([string]::IsNullOrWhiteSpace([string]$profileLookup.development.environmentUrl) -or [string]$profileLookup.development.environmentUrl -notmatch '^https?://') {
    throw "ALM profile 'development' must carry a concrete environmentUrl."
}

foreach ($profileName in @('test', 'enterprise')) {
    $profile = $profileLookup[$profileName]
    $environmentUrl = [string]$profile.environmentUrl
    if ([string]::IsNullOrWhiteSpace($environmentUrl) -or -not ($environmentUrl -match '^replace-(with|after)-')) {
        throw "ALM profile '$profileName' must keep a placeholder environmentUrl in source control."
    }
}

$releaseNotesRoot = Join-Path $root 'artifacts/release-notes'
if (Test-Path -LiteralPath $releaseNotesRoot) {
    $releaseNoteFiles = Get-ChildItem -LiteralPath $releaseNotesRoot -Filter 'RELEASE_NOTES_*.md'
    if ($releaseNoteFiles.Count -eq 0) {
        Write-Warning "Release notes directory exists but no RELEASE_NOTES_*.md files found."
    }
    foreach ($rn in $releaseNoteFiles) {
        $rnContent = Get-Content -LiteralPath $rn.FullName -Raw
        foreach ($required in @('# Release ', '## Summary', '## Deployment Profile', '## Rollback Notes')) {
            if ($rnContent -notmatch [regex]::Escape($required)) {
                throw "Release notes $($rn.Name) missing required section: $required"
            }
        }
        if ($rnContent -match '# Release (\S+)') {
            $rnVersion = $Matches[1]
            if ($rnVersion -notmatch '^\d+\.\d+\.\d+\.\d+$') {
                Write-Warning "Release notes $($rn.Name) has non-standard version format: $rnVersion"
            }
        }
    }
    Write-Host "Release notes validated: $($releaseNoteFiles.Count) file(s) present."
}

if (@($connections.connectionReferences).Count -lt 6) {
    throw "Expected at least six connection references in powerplatform/settings/connection-references.json"
}

foreach ($connection in $connections.connectionReferences) {
    foreach ($property in @('logicalName', 'connectorId', 'purpose', 'ownerStrategy')) {
        if ([string]::IsNullOrWhiteSpace([string]$connection.$property)) {
            throw "Connection reference $($connection.logicalName) is missing property: $property"
        }
    }
}

$envNames = @($envVars.environmentVariables | ForEach-Object { $_.schemaName })
foreach ($requiredEnvVar in @('decisions_TeamsTeamId', 'decisions_TeamsChannelId', 'decisions_SharePointSiteUrl', 'decisions_EnableAiBuilderMinutes', 'decisions_EnableGraphTranscriptSync')) {
    if ($envNames -notcontains $requiredEnvVar) {
        throw "Missing environment variable manifest entry: $requiredEnvVar"
    }
}

Write-Host "ALM activation validation completed."

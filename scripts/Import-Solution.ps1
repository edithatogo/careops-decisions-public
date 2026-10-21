param(
    [string]$SolutionZip = "artifacts/solutions/DecisionsWorkspace/unmanaged/DecisionsWorkspace_unmanaged.zip",
    [string]$SettingsFile,
    [switch]$PublishChanges,
    [switch]$DryRun,
    [string]$TargetProfileName,
    [string]$EnvironmentUrl = $env:POWER_PLATFORM_ENVIRONMENT_URL,
    [string]$AlmProfilesPath = 'powerplatform/settings/alm-profiles.json'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot 'Resolve-AlmProfile.ps1')

function Assert-NoDeploymentSettingsPlaceholders {
    param([Parameter(Mandatory = $true)][string]$Path)

    $settings = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    $placeholders = New-Object System.Collections.Generic.List[string]

    foreach ($entry in @($settings.EnvironmentVariables)) {
        if (Test-AlmPlaceholderValue -Value ([string]$entry.Value)) {
            $placeholders.Add("environment variable $($entry.SchemaName)")
        }
    }

    foreach ($entry in @($settings.ConnectionReferences)) {
        if (Test-AlmPlaceholderValue -Value ([string]$entry.ConnectionId)) {
            $placeholders.Add("connection reference $($entry.LogicalName)")
        }
    }

    if ($placeholders.Count -gt 0) {
        throw "Settings file contains placeholders and will not be imported: $($placeholders -join ', ')"
    }
}

if (-not (Test-Path -LiteralPath $SolutionZip)) {
    throw "Solution zip not found: $SolutionZip"
}

$targetProfile = $null
if (-not [string]::IsNullOrWhiteSpace($TargetProfileName)) {
    $targetProfile = (Resolve-AlmProfile -ProfileName $TargetProfileName -ProfilesPath $AlmProfilesPath).Profile
    if (-not [bool]$targetProfile.allowLiveWrites) {
        throw "ALM profile '$TargetProfileName' does not allow live writes."
    }
}

$resolvedEnvironmentUrl = if ($targetProfile) {
    Resolve-AlmTargetEnvironmentUrl -Profile $targetProfile -EnvironmentUrl $EnvironmentUrl
} elseif (-not [string]::IsNullOrWhiteSpace($EnvironmentUrl) -and -not (Test-AlmPlaceholderValue -Value $EnvironmentUrl)) {
    $EnvironmentUrl
} else {
    $null
}

$target = $null
if (-not [Uri]::TryCreate($resolvedEnvironmentUrl, [UriKind]::Absolute, [ref]$target) -or
    $target.Scheme -ne 'https' -or $target.UserInfo -or $target.Query -or
    $target.Fragment -or $target.AbsolutePath -ne '/') {
    throw 'An exact HTTPS Dataverse environment root is required, including for dry runs. Ambient-target import is disabled.'
}
$resolvedEnvironmentUrl = $target.AbsoluteUri

$arguments = @('solution', 'import', '--path', $SolutionZip, '--environment', $resolvedEnvironmentUrl)
if (-not [string]::IsNullOrWhiteSpace($SettingsFile)) {
    if (-not (Test-Path -LiteralPath $SettingsFile)) {
        throw "Settings file not found: $SettingsFile"
    }
    Assert-NoDeploymentSettingsPlaceholders -Path $SettingsFile
    $arguments += @('--settings-file', $SettingsFile)
}
if ($PublishChanges) {
    $arguments += '--publish-changes'
}

if ($DryRun) {
    Write-Host ("Dry run: pac {0}" -f ($arguments -join ' '))
    return
}

if (-not (Get-Command pac -ErrorAction SilentlyContinue)) {
    throw "Power Platform CLI 'pac' was not found on PATH."
}
# Preserve the caller's already-qualified session and selected profile. A stored
# profile alone is not target access; a failed read must prevent the import.
$identity = & pac org who --environment $resolvedEnvironmentUrl 2>&1
if ($LASTEXITCODE -ne 0 -or -not ($identity | Out-String).Trim()) {
    throw 'Exact-target identity/access read failed. Preserve the profile and reconcile its qualified route before import.'
}
& pac @arguments
if ($LASTEXITCODE -ne 0) {
    throw "PAC solution import failed for $SolutionZip."
}

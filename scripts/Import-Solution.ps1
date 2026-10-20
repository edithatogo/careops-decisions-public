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

if (-not (Get-Command pac -ErrorAction SilentlyContinue)) {
    throw "Power Platform CLI 'pac' was not found on PATH."
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

if ($resolvedEnvironmentUrl) {
    Write-Host "Activating PAC auth for environment: $resolvedEnvironmentUrl"
    Select-PacAuthEnvironment -EnvironmentUrl $resolvedEnvironmentUrl | Out-Null
}

$arguments = @('solution', 'import', '--path', $SolutionZip)
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

& pac @arguments
if ($LASTEXITCODE -ne 0) {
    throw "PAC solution import failed for $SolutionZip."
}

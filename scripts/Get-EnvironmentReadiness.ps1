param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [switch]$Json,
    [switch]$DryRun,
    [string]$TargetProfileName,
    [string]$EnvironmentUrl = $env:POWER_PLATFORM_ENVIRONMENT_URL,
    [string]$AlmProfilesPath = 'powerplatform/settings/alm-profiles.json',
    [string]$DeploymentSettingsPath = 'powerplatform/settings/deployment-settings.sample.json'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot 'Resolve-AlmProfile.ps1')

function Test-CommandAvailable {
    param([string]$Name)
    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($command) {
        return [pscustomobject]@{
            name = $Name
            available = $true
            source = $command.Source
        }
    }

    [pscustomobject]@{
        name = $Name
        available = $false
        source = $null
    }
}

function Get-PacAuthState {
    $state = [pscustomobject]@{
        ExitCode = $null
        RawOutput = $null
        ActiveEnvironment = $null
        ActiveIndex = $null
        Error = $null
    }

    if ($DryRun) {
        $state.Error = 'Authentication state is not inspected during an offline dry run.'
        return $state
    }
    if (-not (Get-Command pac -ErrorAction SilentlyContinue)) {
        $state.Error = "pac is not available"
        return $state
    }

    try {
        $catalog = Get-PacAuthEntries
        $state.ExitCode = 0
        $state.RawOutput = $catalog.RawOutput
        $activeEntry = $catalog.Entries | Where-Object { $_.Active } | Select-Object -First 1
        if ($activeEntry) {
            $state.ActiveIndex = $activeEntry.Index
            $state.ActiveEnvironment = $activeEntry.EnvironmentUrl
        }
    } catch {
        $state.ExitCode = 1
        $state.Error = $_.Exception.Message
        $state.RawOutput = $state.Error
    }

    return $state
}

$repoRoot = (Resolve-Path $RepoRoot).Path
$deploymentSettingsPath = if ([System.IO.Path]::IsPathRooted($DeploymentSettingsPath)) { $DeploymentSettingsPath } else { Join-Path $repoRoot $DeploymentSettingsPath }
$flowCatalogPath = Join-Path $repoRoot 'powerplatform/src/flows/flow-catalog.yaml'
$targetProfile = $null
if (-not [string]::IsNullOrWhiteSpace($TargetProfileName)) {
    $targetProfile = (Resolve-AlmProfile -RepoRoot $repoRoot -ProfilesPath $AlmProfilesPath -ProfileName $TargetProfileName).Profile
}

$toolChecks = @(
    Test-CommandAvailable -Name 'pac'
    Test-CommandAvailable -Name 'pacx'
    Test-CommandAvailable -Name 'git'
    Test-CommandAvailable -Name 'node'
    Test-CommandAvailable -Name 'npm'
    Test-CommandAvailable -Name 'dotnet'
)

$deploymentSettings = $null
if (Test-Path -LiteralPath $deploymentSettingsPath) {
    $deploymentSettings = Get-Content -LiteralPath $deploymentSettingsPath -Raw | ConvertFrom-Json
}

$expectedEnvironmentVariables = @(
    'decisions_TeamsTeamId',
    'decisions_TeamsChannelId',
    'decisions_PlannerPlanId',
    'decisions_SharePointSiteUrl',
    'decisions_EnableAiBuilderMinutes',
    'decisions_AiBuilderPromptName',
    'decisions_IntegrationLogRetentionDays'
)

$expectedConnectionReferences = @(
    'decisions_sharedcommondataserviceforapps',
    'decisions_sharedteams',
    'decisions_sharedplanner',
    'decisions_sharedsharepointonline',
    'decisions_sharedoffice365users',
    'decisions_sharedoffice365',
    'decisions_sharedaibuilder'
)

$expectedConnectors = @(
    'Dataverse',
    'Microsoft Teams',
    'Planner',
    'SharePoint',
    'Office 365 Users',
    'Office 365 Outlook',
    'AI Builder'
)

$environmentVariableNames = @()
$connectionReferenceNames = @()
$placeholderEnvironmentVariables = @()
$placeholderConnectionReferences = @()
$missingExpectedEnvironmentVariables = @()
$missingExpectedConnectionReferences = @()
$aiBuilderMinutesValue = $null

if ($deploymentSettings) {
    $environmentVariableNames = @($deploymentSettings.EnvironmentVariables | ForEach-Object { $_.SchemaName })
    $connectionReferenceNames = @($deploymentSettings.ConnectionReferences | ForEach-Object { $_.LogicalName })
    $missingExpectedEnvironmentVariables = @($expectedEnvironmentVariables | Where-Object { $_ -notin $environmentVariableNames })
    $missingExpectedConnectionReferences = @($expectedConnectionReferences | Where-Object { $_ -notin $connectionReferenceNames })

    foreach ($entry in @($deploymentSettings.EnvironmentVariables)) {
        if ($entry.SchemaName -eq 'decisions_EnableAiBuilderMinutes') {
            $aiBuilderMinutesValue = [string]$entry.Value
        }

        if (Test-AlmPlaceholderValue -Value ([string]$entry.Value)) {
            $placeholderEnvironmentVariables += $entry.SchemaName
        }
    }

    foreach ($entry in @($deploymentSettings.ConnectionReferences)) {
        if (Test-AlmPlaceholderValue -Value ([string]$entry.ConnectionId)) {
            $placeholderConnectionReferences += $entry.LogicalName
        }
    }
}

$aiBuilderMinutesEnabled = $false
if (-not [string]::IsNullOrWhiteSpace($aiBuilderMinutesValue)) {
    $aiBuilderMinutesEnabled = $aiBuilderMinutesValue -match '^(?i:true|1|yes)$'
}

$operationalSupportReadiness = @(
    [pscustomobject]@{
        Area = 'Connector references'
        Status = if ($missingExpectedConnectionReferences.Count -eq 0 -and $placeholderConnectionReferences.Count -eq 0) { 'Ready' } else { 'Blocked' }
        Details = if ($missingExpectedConnectionReferences.Count -eq 0 -and $placeholderConnectionReferences.Count -eq 0) { 'Expected connection references are present and no placeholder connection IDs were detected.' } else { 'Missing: ' + ($(if ($missingExpectedConnectionReferences.Count -gt 0) { $missingExpectedConnectionReferences -join ', ' } else { 'none' })) + '; placeholders: ' + ($(if ($placeholderConnectionReferences.Count -gt 0) { $placeholderConnectionReferences -join ', ' } else { 'none' })) }
        SupportAction = 'Create or repair maker connections, regenerate ignored local settings, then re-run readiness before import.'
    },
    [pscustomobject]@{
        Area = 'Environment variables'
        Status = if ($missingExpectedEnvironmentVariables.Count -eq 0 -and $placeholderEnvironmentVariables.Count -eq 0) { 'Ready' } else { 'Blocked' }
        Details = if ($missingExpectedEnvironmentVariables.Count -eq 0 -and $placeholderEnvironmentVariables.Count -eq 0) { 'Expected environment variables are present and no placeholder values were detected.' } else { 'Missing: ' + ($(if ($missingExpectedEnvironmentVariables.Count -gt 0) { $missingExpectedEnvironmentVariables -join ', ' } else { 'none' })) + '; placeholders: ' + ($(if ($placeholderEnvironmentVariables.Count -gt 0) { $placeholderEnvironmentVariables -join ', ' } else { 'none' })) }
        SupportAction = 'Populate real Teams, Planner, SharePoint, and feature-flag values in the ignored local settings file before live import.'
    },
    [pscustomobject]@{
        Area = 'Graph permissions'
        Status = 'ManualReview'
        Details = 'This read-only script does not call Graph. Treat 403 responses from group, channel, site, or Planner calls as live activation blockers.'
        SupportAction = 'Capture endpoint, object ID, account, and required permission before escalating for tenant/admin approval.'
    },
    [pscustomobject]@{
        Area = 'AI human review'
        Status = if ($aiBuilderMinutesEnabled) { 'ManualReview' } else { 'GatedOff' }
        Details = if ($aiBuilderMinutesEnabled) { 'AI Builder minutes are enabled in settings; human review evidence must be confirmed before publishing AI output.' } else { 'AI Builder minutes are disabled or not concretely enabled.' }
        SupportAction = 'Keep AI output in draft until reviewer, review time, prompt/run evidence, and accept/edit/reject state are recorded.'
    }
)

$pacState = Get-PacAuthState
$pacCommand = Get-Command pac -ErrorAction SilentlyContinue
$pacxCommand = Get-Command pacx -ErrorAction SilentlyContinue
$resolvedEnvironmentUrl = $null
if ($targetProfile) {
    $resolvedEnvironmentUrl = Resolve-AlmTargetEnvironmentUrl -Profile $targetProfile -EnvironmentUrl $EnvironmentUrl
}

$checks = @(
    [pscustomobject]@{
        Name = 'pac CLI is available'
        Status = if ($pacCommand) { 'Pass' } else { 'Fail' }
        Details = if ($pacCommand) { "Found at $($pacCommand.Source)" } else { "Power Platform CLI 'pac' was not found on PATH." }
        Recommendation = 'Use pac for solution pack, check, and import commands.'
    },
    [pscustomobject]@{
        Name = 'Stored PAC profile has a selected environment (not access proof)'
        Status = if ($DryRun) { 'Skip' } elseif ($pacState.ActiveEnvironment) { 'Pass' } elseif ($pacState.ExitCode -eq 0) { 'Warn' } else { 'Fail' }
        Details = if ($pacState.ActiveEnvironment) { "Active environment: $($pacState.ActiveEnvironment)" } elseif ($pacState.Error) { $pacState.Error } else { 'No active environment was parsed from pac auth list.' }
        Recommendation = 'Run pac auth select before targeting a tenant.'
    },
    [pscustomobject]@{
        Name = 'Target ALM profile is resolved'
        Status = if ($targetProfile) { 'Pass' } else { 'Skip' }
        Details = if ($targetProfile) { "Profile: $($targetProfile.name)" } else { 'No target profile was requested.' }
        Recommendation = 'Pass -TargetProfileName when you want activation proof against a named profile.'
    },
    [pscustomobject]@{
        Name = 'Target ALM profile allows live writes'
        Status = if ($targetProfile -and [bool]$targetProfile.allowLiveWrites) { 'Pass' } elseif ($targetProfile) { 'Fail' } else { 'Skip' }
        Details = if ($targetProfile) { "allowLiveWrites = $($targetProfile.allowLiveWrites)" } else { 'No target profile was requested.' }
        Recommendation = 'Use a profile that explicitly allows live writes for activation and import.'
    },
    [pscustomobject]@{
        Name = 'Target environment URL is concrete'
        Status = if ($resolvedEnvironmentUrl) { 'Pass' } elseif ($targetProfile) { 'Fail' } else { 'Skip' }
        Details = if ($resolvedEnvironmentUrl) { $resolvedEnvironmentUrl } elseif ($targetProfile) { 'Target profile does not resolve to a concrete environment URL.' } else { 'No target profile was requested.' }
        Recommendation = 'Set POWER_PLATFORM_ENVIRONMENT_URL or pass -EnvironmentUrl when using a target profile.'
    },
    [pscustomobject]@{
        Name = 'pacx is available'
        Status = if ($pacxCommand) { 'Pass' } else { 'Warn' }
        Details = if ($pacxCommand) { "Found at $($pacxCommand.Source)" } else { 'pacx was not found on PATH.' }
        Recommendation = 'Use pacx for Dataverse work when installed.'
    },
    [pscustomobject]@{
        Name = 'Deployment settings exist'
        Status = if ($deploymentSettings) { 'Pass' } else { 'Fail' }
        Details = if ($deploymentSettings) { "$DeploymentSettingsPath is present." } else { "Missing $DeploymentSettingsPath." }
        Recommendation = 'Restore the sample deployment settings file.'
    },
    [pscustomobject]@{
        Name = 'Expected environment variables are present'
        Status = if (@($expectedEnvironmentVariables | Where-Object { $_ -notin $environmentVariableNames }).Count -eq 0) { 'Pass' } else { 'Fail' }
        Details = if (@($expectedEnvironmentVariables | Where-Object { $_ -notin $environmentVariableNames }).Count -eq 0) { 'All expected environment variables were found.' } else { 'Missing: ' + (@($expectedEnvironmentVariables | Where-Object { $_ -notin $environmentVariableNames }) -join ', ') }
        Recommendation = 'Add any missing environment variable entries to the deployment settings sample.'
    },
    [pscustomobject]@{
        Name = 'Environment variable values are not scaffold placeholders'
        Status = if ($placeholderEnvironmentVariables.Count -eq 0) { 'Pass' } else { 'Warn' }
        Details = if ($placeholderEnvironmentVariables.Count -eq 0) { 'No obvious placeholder environment variable values were detected.' } else { 'Placeholder values remain for: ' + ($placeholderEnvironmentVariables -join ', ') }
        Recommendation = 'Replace placeholder environment values before a live import.'
    },
    [pscustomobject]@{
        Name = 'Expected connection references are present'
        Status = if (@($expectedConnectionReferences | Where-Object { $_ -notin $connectionReferenceNames }).Count -eq 0) { 'Pass' } else { 'Fail' }
        Details = if (@($expectedConnectionReferences | Where-Object { $_ -notin $connectionReferenceNames }).Count -eq 0) { 'All expected connection references were found.' } else { 'Missing: ' + (@($expectedConnectionReferences | Where-Object { $_ -notin $connectionReferenceNames }) -join ', ') }
        Recommendation = 'Add any missing connection reference entries to the deployment settings sample.'
    },
    [pscustomobject]@{
        Name = 'Connection reference IDs are not scaffold placeholders'
        Status = if ($placeholderConnectionReferences.Count -eq 0) { 'Pass' } else { 'Warn' }
        Details = if ($placeholderConnectionReferences.Count -eq 0) { 'No obvious placeholder connection reference IDs were detected.' } else { 'Placeholder connection IDs remain for: ' + ($placeholderConnectionReferences -join ', ') }
        Recommendation = 'Replace placeholder connection IDs before import.'
    }
)

$result = [pscustomobject]@{
    GeneratedOn = (Get-Date).ToString('o')
    RepoRoot = $repoRoot
    Tools = $toolChecks
    NativeAccessVerified = $false
    QualificationScope = 'Local configuration/preflight only; stored profiles do not establish native access or import authority.'
    PacAvailable = [bool]$pacCommand
    PacxAvailable = [bool]$pacxCommand
    PacActiveEnvironment = $pacState.ActiveEnvironment
    TargetProfileName = if ($targetProfile) { $targetProfile.name } else { $null }
    TargetEnvironmentUrl = $resolvedEnvironmentUrl
    DeploymentSettingsPresent = [bool]$deploymentSettings
    EnvironmentVariableNames = $environmentVariableNames
    ConnectionReferenceNames = $connectionReferenceNames
    ExpectedConnectors = $expectedConnectors
    ExpectedEnvironmentVariables = $expectedEnvironmentVariables
    ExpectedConnectionReferences = $expectedConnectionReferences
    PlaceholderEnvironmentVariables = $placeholderEnvironmentVariables
    PlaceholderConnectionReferences = $placeholderConnectionReferences
    MissingExpectedEnvironmentVariables = $missingExpectedEnvironmentVariables
    MissingExpectedConnectionReferences = $missingExpectedConnectionReferences
    AiBuilderMinutesEnabled = $aiBuilderMinutesEnabled
    OperationalSupportReadiness = $operationalSupportReadiness
    PacAuthList = $pacState.RawOutput
    Checks = $checks
    NextCommands = @(
        'pac auth list',
        'Verify the existing qualified account/tenant/session without changing another owner''s profile.',
        'pac solution pack --folder powerplatform/solutions/DecisionsWorkspace --zipfile artifacts/solutions/DecisionsWorkspace_unmanaged.zip --packagetype Unmanaged',
        'pac solution check --path artifacts/solutions/DecisionsWorkspace_unmanaged.zip',
        './scripts/Import-Solution.ps1 -DryRun -EnvironmentUrl <reviewed-exact-HTTPS-root> -SettingsFile <reviewed-ignored-local-settings>'
    )
    Readiness = if ($checks | Where-Object { $_.Status -eq 'Fail' }) { 'Not ready' } elseif ($checks | Where-Object { $_.Status -eq 'Warn' }) { 'Ready with warnings' } else { 'Ready' }
    DryRun = [bool]$DryRun
}

if ($Json) {
    $result | ConvertTo-Json -Depth 8
    return
}

Write-Host "Local environment preflight; native access is not verified"
Write-Host "Repository: $repoRoot"
Write-Host ""
Write-Host "Tools"
$toolChecks | ForEach-Object {
    $status = if ($_.available) { 'available' } else { 'missing' }
    Write-Host "- $($_.name): $status $($_.source)"
}
Write-Host ""
Write-Host "Deployment settings present: $($result.deploymentSettingsPresent)"
Write-Host "Environment variables: $($result.environmentVariableNames -join ', ')"
Write-Host "Connection references: $($result.connectionReferenceNames -join ', ')"
Write-Host ""
Write-Host "PAC auth list"
Write-Host $pacState.RawOutput
Write-Host ""
Write-Host "Next commands"
$result.NextCommands | ForEach-Object { Write-Host "- $_" }

return $result

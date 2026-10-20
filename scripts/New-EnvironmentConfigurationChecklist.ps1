param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [string]$OutputPath = 'docs/deployment/environment-configuration-wizard.md',
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

function Read-JsonFile {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Missing file: $Path"
    }

    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}

function Get-DeploymentSettingsValue {
    param(
        [object]$DeploymentSettings,
        [string]$SchemaName
    )

    $deploymentSchemaName = switch ($SchemaName) {
        'dft_DefaultTeamId' { 'decisions_TeamsTeamId' }
        'dft_DefaultChannelId' { 'decisions_TeamsChannelId' }
        'dft_DefaultPlannerPlanId' { 'decisions_PlannerPlanId' }
        'dft_DefaultSharePointSiteUrl' { 'decisions_SharePointSiteUrl' }
        'dft_MinutesLibraryName' { 'decisions_MinutesLibraryName' }
        'dft_EnableAIBuilderMinutes' { 'decisions_EnableAiBuilderMinutes' }
        'dft_EnableGraphTranscriptSync' { 'decisions_EnableGraphTranscriptSync' }
        'dft_IntegrationLogRetentionDays' { 'decisions_IntegrationLogRetentionDays' }
        default { $SchemaName }
    }

    foreach ($entry in @($DeploymentSettings.EnvironmentVariables)) {
        if ($entry.SchemaName -eq $deploymentSchemaName) {
            return [string]$entry.Value
        }
    }

    return $null
}

function Test-PlaceholderValue {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $true
    }

    return (
        $Value -match '(^|/|:)replace-with-' -or
        $Value -match '^00000000-0000-0000-0000-000000000000$' -or
        $Value -match '^planner-plan-id$' -or
        $Value -match '^https?://contoso\.sharepoint\.com(/|$)'
    )
}

function New-WizardMarkdown {
    param(
        [object]$EnvironmentVariableManifest,
        [object]$ConnectionReferenceManifest,
        [object]$DeploymentSettings
    )

    $lines = New-Object System.Collections.Generic.List[string]
    [void]$lines.Add('# Environment Configuration Wizard')
    [void]$lines.Add('')
    [void]$lines.Add('This repo-local checklist is generated from the settings manifests and the sample deployment settings file.')
    [void]$lines.Add('')
    [void]$lines.Add('Use it to confirm the values that must be present before packaging, import, or smoke testing.')
    [void]$lines.Add('')
    [void]$lines.Add('Use the generator to refresh this file from the repo state:')
    [void]$lines.Add('')
    [void]$lines.Add('```powershell')
    [void]$lines.Add('powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-EnvironmentConfigurationChecklist.ps1')
    [void]$lines.Add('```')
    [void]$lines.Add('')

    [void]$lines.Add('## Configuration Checklist')
    [void]$lines.Add('')
    [void]$lines.Add('- [ ] Environment variables are populated in `powerplatform/settings/deployment-settings.sample.json`.')
    [void]$lines.Add('- [ ] Connection references are populated in `powerplatform/settings/deployment-settings.sample.json`.')
    [void]$lines.Add('- [ ] Teams team and channel IDs point to the intended workspace.')
    [void]$lines.Add('- [ ] Planner plan ID points to the intended action plan, if Planner sync is enabled.')
    [void]$lines.Add('- [ ] SharePoint site URL points to the document library host for agendas, papers, minutes, transcripts, and packs.')
    [void]$lines.Add('- [ ] AI Builder minutes toggle stays disabled until licensing, capacity, and review governance are confirmed.')
    [void]$lines.Add('- [ ] Placeholder values are removed before any live import.')
    [void]$lines.Add('')

    [void]$lines.Add('## Environment Variables')
    [void]$lines.Add('')
    [void]$lines.Add('| Schema name | Purpose | Required for MVP | Sample value | Status |')
    [void]$lines.Add('| --- | --- | --- | --- | --- |')

    foreach ($entry in @($EnvironmentVariableManifest.environmentVariables)) {
        $sampleValue = Get-DeploymentSettingsValue -DeploymentSettings $DeploymentSettings -SchemaName $entry.schemaName
        if ($null -eq $sampleValue) {
            $sampleValue = '(missing)'
        }

        $status = if (Test-PlaceholderValue -Value $sampleValue) { 'Needs review' } else { 'Ready' }
        [void]$lines.Add('| `' + $entry.schemaName + '` | ' + $entry.purpose + ' | ' + ($(if ($entry.requiredForMvp) { 'Yes' } else { 'No' })) + ' | `' + $sampleValue + '` | ' + $status + ' |')
    }

    [void]$lines.Add('')
    [void]$lines.Add('## Connection References')
    [void]$lines.Add('')
    [void]$lines.Add('| Logical name | Connector | Purpose | Required for MVP | Owner strategy | Status |')
    [void]$lines.Add('| --- | --- | --- | --- | --- | --- |')

    foreach ($entry in @($ConnectionReferenceManifest.connectionReferences)) {
        $status = if ($DeploymentSettings) {
            $match = @($DeploymentSettings.ConnectionReferences | Where-Object { $_.LogicalName -eq $entry.logicalName } | Select-Object -First 1)
            if ($match) {
                if (Test-PlaceholderValue -Value ([string]$match.ConnectionId)) { 'Needs review' } else { 'Ready' }
            } else {
                'Missing'
            }
        } else {
            'Unknown'
        }

        [void]$lines.Add('| `' + $entry.logicalName + '` | `' + $entry.connectorId + '` | ' + $entry.purpose + ' | ' + ($(if ($entry.requiredForMvp) { 'Yes' } else { 'No' })) + ' | ' + $entry.ownerStrategy + ' | ' + $status + ' |')
    }

    [void]$lines.Add('')
    [void]$lines.Add('## Targeted Readiness Checks')
    [void]$lines.Add('')
    [void]$lines.Add('### Teams')
    [void]$lines.Add('')
    [void]$lines.Add('- [ ] `decisions_TeamsTeamId` is set to the intended team.')
    [void]$lines.Add('- [ ] `decisions_TeamsChannelId` is set to the intended channel.')
    [void]$lines.Add('- [ ] The Teams connection reference resolves to the correct maker or service connection.')
    [void]$lines.Add('')
    [void]$lines.Add('### Planner')
    [void]$lines.Add('')
    [void]$lines.Add('- [ ] `decisions_PlannerPlanId` points to the intended plan when Planner sync is in scope.')
    [void]$lines.Add('- [ ] The Planner connection reference resolves and has access to the target plan.')
    [void]$lines.Add('- [ ] Planner can remain unset when the deployment does not use task sync.')
    [void]$lines.Add('')
    [void]$lines.Add('### SharePoint')
    [void]$lines.Add('')
    [void]$lines.Add('- [ ] `decisions_SharePointSiteUrl` points to the correct site.')
    [void]$lines.Add('- [ ] `decisions_MinutesLibraryName` points to the intended document library.')
    [void]$lines.Add('- [ ] The SharePoint connection reference resolves to an account with the right library access.')
    [void]$lines.Add('')
    [void]$lines.Add('### AI Builder')
    [void]$lines.Add('')
    [void]$lines.Add('- [ ] `decisions_EnableAiBuilderMinutes` stays `false` unless licensing and capacity are confirmed.')
    [void]$lines.Add('- [ ] `decisions_AiBuilderPromptName` matches the prompt published in the target environment.')
    [void]$lines.Add('- [ ] The Dataverse connection reference used for AI Builder has the required rights.')
    [void]$lines.Add('- [ ] Human review remains part of the transcript-to-minutes path.')
    [void]$lines.Add('')

    [void]$lines.Add('## Suggested Safe Order')
    [void]$lines.Add('')
    [void]$lines.Add('1. Confirm the deployment settings file is present.')
    [void]$lines.Add('2. Populate environment variables and replace placeholder values.')
    [void]$lines.Add('3. Map connection references to the intended maker or service connections.')
    [void]$lines.Add('4. Confirm Teams, Planner, and SharePoint targets.')
    [void]$lines.Add('5. Leave AI Builder disabled until capacity and governance are signed off.')
    [void]$lines.Add('6. Re-run the readiness scripts before any import.')
    [void]$lines.Add('')
    [void]$lines.Add('```powershell')
    [void]$lines.Add('powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Get-EnvironmentReadiness.ps1')
    [void]$lines.Add('powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-EnvironmentConfigurationChecklist.ps1')
    [void]$lines.Add('```')

    return ($lines -join [System.Environment]::NewLine)
}

$repoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
$environmentVariableManifestPath = Join-Path $repoRoot 'powerplatform/settings/environment-variable-manifest.json'
$connectionReferenceManifestPath = Join-Path $repoRoot 'powerplatform/settings/connection-references.json'
$deploymentSettingsPath = Join-Path $repoRoot 'powerplatform/settings/deployment-settings.sample.json'

$environmentVariableManifest = Read-JsonFile -Path $environmentVariableManifestPath
$connectionReferenceManifest = Read-JsonFile -Path $connectionReferenceManifestPath
$deploymentSettings = Read-JsonFile -Path $deploymentSettingsPath

$markdown = New-WizardMarkdown -EnvironmentVariableManifest $environmentVariableManifest -ConnectionReferenceManifest $connectionReferenceManifest -DeploymentSettings $deploymentSettings
$outputFullPath = if ([System.IO.Path]::IsPathRooted($OutputPath)) { $OutputPath } else { Join-Path $repoRoot $OutputPath }

if (-not $DryRun) {
    $outputDir = Split-Path -Parent $outputFullPath
    if ($outputDir -and -not (Test-Path -LiteralPath $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }

    Set-Content -LiteralPath $outputFullPath -Value $markdown -Encoding utf8
}

[pscustomobject]@{
    OutputPath = $outputFullPath
    DryRun     = [bool]$DryRun
    EnvironmentVariableCount = @($environmentVariableManifest.environmentVariables).Count
    ConnectionReferenceCount = @($connectionReferenceManifest.connectionReferences).Count
    Markdown   = $markdown
}

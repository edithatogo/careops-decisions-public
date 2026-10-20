[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

function Get-RepoPath {
    param([Parameter(Mandatory = $true)][string]$RelativePath)
    return Join-Path $repoRoot $RelativePath
}

function Assert-FileContains {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string[]]$Patterns
    )

    $fullPath = Get-RepoPath $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw "Missing security artifact: $RelativePath"
    }

    $content = Get-Content -LiteralPath $fullPath -Raw
    if ([string]::IsNullOrWhiteSpace($content)) {
        throw "Security artifact is empty: $RelativePath"
    }

    foreach ($pattern in $Patterns) {
        if ($content -notmatch [regex]::Escape($pattern)) {
            throw "$RelativePath does not reference required text: $pattern"
        }
    }

    return $content
}

$schemaPath = Get-RepoPath 'powerplatform/src/schema/dataverse-tables.yaml'
if (-not (Test-Path -LiteralPath $schemaPath)) {
    throw 'Dataverse table schema not found.'
}

$schemaText = Get-Content -LiteralPath $schemaPath -Raw
$schemaTables = [regex]::Matches($schemaText, '(?m)^  - logicalName:\s+(dft_[A-Za-z0-9_]+)\s*$') |
    ForEach-Object { $_.Groups[1].Value } |
    Sort-Object -Unique

if ($schemaTables.Count -lt 1) {
    throw 'No dft_* tables were found in the Dataverse schema.'
}

$roleMatrixText = Assert-FileContains -RelativePath 'docs/security/security-role-matrix.yaml' -Patterns @(
    'schema: decisions.workspace.security-role-matrix.v1',
    'Restricted Reader',
    'Guest Presenter',
    'Service Account',
    'tablePrivileges:',
    'classificationModel:',
    'auditStrategy:'
)

$matrixTables = [regex]::Matches($roleMatrixText, '(?m)^\s*-\s+table:\s+(dft_[A-Za-z0-9_]+)\s*$') |
    ForEach-Object { $_.Groups[1].Value } |
    Sort-Object -Unique

$missingTables = @($schemaTables | Where-Object { $_ -notin $matrixTables })
if ($missingTables.Count -gt 0) {
    throw "Security role matrix is missing table privilege entries: $($missingTables -join ', ')"
}

$majorSensitiveTables = @(
    'dft_AccessRule',
    'dft_AIBuilderPrompt',
    'dft_AIBuilderRun',
    'dft_Attendee',
    'dft_IntegrationLog',
    'dft_Minutes',
    'dft_Paper',
    'dft_Transcript'
)

foreach ($table in $majorSensitiveTables) {
    if ($roleMatrixText -notmatch [regex]::Escape($table)) {
        throw "Security role matrix does not reference sensitive table: $table"
    }
}

Assert-FileContains -RelativePath 'docs/security/dlp-policy-assumptions.md' -Patterns @(
    'Office 365 Outlook',
    'Microsoft Teams',
    'Planner',
    'SharePoint',
    'AI Builder',
    'Microsoft Graph',
    'Microsoft Entra ID'
) | Out-Null

Assert-FileContains -RelativePath 'docs/security/retention-privacy-rules.md' -Patterns @(
    'Agenda',
    'Papers',
    'Minutes',
    'Transcript',
    'AI And Integration Records',
    'dft_IntegrationLog',
    'Privacy'
) | Out-Null

Assert-FileContains -RelativePath 'docs/security/external-participant-policy.md' -Patterns @(
    'Guest Presenter',
    'Restricted Reader',
    'dft_AccessRule',
    '30 days',
    'External'
) | Out-Null

Assert-FileContains -RelativePath 'docs/security/threat-model.md' -Patterns @(
    'Connector',
    'AI',
    'Oversharing',
    'Stale permissions',
    'Maker'
) | Out-Null

Write-Host "Security contract validation passed. ($($schemaTables.Count) schema tables covered)"

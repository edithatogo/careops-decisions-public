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
        throw "Missing operations artifact: $RelativePath"
    }

    $content = Get-Content -LiteralPath $fullPath -Raw
    if ([string]::IsNullOrWhiteSpace($content)) {
        throw "Operations artifact is empty: $RelativePath"
    }

    foreach ($pattern in $Patterns) {
        if ($content -notmatch [regex]::Escape($pattern)) {
            throw "$RelativePath does not reference required text: $pattern"
        }
    }

    return $content
}

$supportRunbooks = Assert-FileContains -RelativePath 'docs/operations/support-runbooks.md' -Patterns @(
    'Runbook: Failed Flows',
    'Runbook: Stale Sync',
    'Runbook: Missing Files',
    'Runbook: Stuck Approvals',
    'Runbook: Unresolved Attendees',
    'Runbook: Stale External IDs',
    'Runbook: Orphaned Planner Tasks',
    'Runbook: AI Review Exceptions',
    'Runbook: Data-Quality Exceptions',
    'Runbook: Access-Rule Errors',
    'Detection',
    'Triage',
    'Escalation'
)

Assert-FileContains -RelativePath 'docs/operations/monitoring-requirements.md' -Patterns @(
    'Flow health',
    'Sync staleness',
    'Approval progress',
    'AI review queue',
    'Data quality',
    'Access exceptions',
    'Action drift',
    'Person data',
    'Operational Alert Fields',
    'Owner',
    'Severity',
    'Evidence',
    'Resolution',
    'Source record',
    'Correlation ID',
    'dft_IntegrationLog'
) | Out-Null

Assert-FileContains -RelativePath 'docs/operations/business-continuity-export.md' -Patterns @(
    'Meeting Pack',
    'Agenda Items',
    'Minutes',
    'Decision Register',
    'Action Register',
    'Dataverse Native Export',
    'Power Automate Export',
    'Restoration Path',
    'Recovery Point Objective',
    'Recovery Time Objective'
) | Out-Null

Assert-FileContains -RelativePath 'docs/operations/support-roles-and-escalation.md' -Patterns @(
    'Secretary',
    'Chair',
    'Governance Administrator',
    'Power Platform Admin',
    'SharePoint Owner',
    'Planner Plan Owner',
    'Severity',
    'Tier 1',
    'Tier 2',
    'Tier 3'
) | Out-Null

Assert-FileContains -RelativePath 'docs/deployment/release-artifacts.md' -Patterns @(
    'Operational Owner',
    'Rollback Notes',
    'Rollback Documentation',
    'Rollback method',
    'Previous solution zip is archived and accessible'
) | Out-Null

$runbookCount = ([regex]::Matches($supportRunbooks, '(?m)^## Runbook: ')).Count
if ($runbookCount -lt 10) {
    throw "Expected at least 10 support runbooks; found $runbookCount."
}

Write-Host "Operations contract validation passed. ($runbookCount support runbooks covered)"

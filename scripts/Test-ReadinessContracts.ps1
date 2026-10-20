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
        throw "Missing readiness artifact: $RelativePath"
    }

    $content = Get-Content -LiteralPath $fullPath -Raw
    if ([string]::IsNullOrWhiteSpace($content)) {
        throw "Readiness artifact is empty: $RelativePath"
    }

    foreach ($pattern in $Patterns) {
        if ($content -notmatch [regex]::Escape($pattern)) {
            throw "$RelativePath does not reference required text: $pattern"
        }
    }

    return $content
}

$adrIndex = Assert-FileContains -RelativePath 'docs/adr/README.md' -Patterns @(
    '0001-dataverse-system-of-record.md',
    '0002-alm-first-delivery.md',
    '0003-model-driven-plus-teams-canvas.md',
    '0004-ai-human-review-gate.md',
    '0005-graph-azure-entra-deferred.md',
    '0006-power-bi-deferred-until-stable-data.md',
    '0007-feature-switchboard-pattern.md',
    '0008-service-account-connection-ownership.md',
    '0009-dataverse-first-model-security.md',
    '0010-document-storage-strategy.md'
)

$adrFiles = Get-ChildItem -LiteralPath (Get-RepoPath 'docs/adr') -Filter '0*.md' |
    Sort-Object Name

if ($adrFiles.Count -lt 10) {
    throw "Expected at least 10 ADR files; found $($adrFiles.Count)."
}

foreach ($adrFile in $adrFiles) {
    $relativePath = "docs/adr/$($adrFile.Name)"
    $adrText = Assert-FileContains -RelativePath $relativePath -Patterns @(
        '## Status',
        '## Context',
        '## Decision',
        '## Consequences'
    )

    if ($adrText -notmatch '(?m)^Accepted\.?\s*$') {
        throw "$relativePath must have an Accepted status line."
    }

    if ($adrIndex -notmatch [regex]::Escape($adrFile.Name)) {
        throw "ADR index does not list $($adrFile.Name)."
    }
}

Assert-FileContains -RelativePath 'docs/readiness/licensing-capacity-matrix.md' -Patterns @(
    'Status key',
    'Confirmed',
    'Assumed',
    'Dataverse or Dataverse for Teams',
    'Power Apps',
    'Power Automate',
    'AI Builder',
    'Planner',
    'Teams',
    'Outlook',
    'SharePoint',
    'Power BI/Fabric',
    'Microsoft Graph',
    'Azure',
    'Entra',
    'Confirm each capability'
) | Out-Null

Assert-FileContains -RelativePath 'docs/readiness/tenant-permissions-matrix.md' -Patterns @(
    'Power Platform maker',
    'Environment admin',
    'Dataverse system customizer/admin',
    'Connection owner',
    'Teams owner',
    'SharePoint owner',
    'Planner plan owner',
    'AI Builder admin/capacity owner',
    'Graph admin consent',
    'App registration admin',
    'Azure RBAC owner/contributor',
    'Entra group admin'
) | Out-Null

Assert-FileContains -RelativePath 'docs/readiness/service-account-strategy.md' -Patterns @(
    'MVP Default',
    'Shared Environment Target',
    'Required Connection Ownership Records',
    'Outlook invite sync connection owner',
    'Teams notification connection owner',
    'Planner task sync connection owner',
    'SharePoint document registration connection owner',
    'Office 365 Users hierarchy sync connection owner',
    'AI Builder prompt runner owner',
    'app registrations',
    'managed identities',
    'service principals'
) | Out-Null

Assert-FileContains -RelativePath 'docs/readiness/implementation-readiness-gates.md' -Patterns @(
    'Local Gate',
    'Power Platform Development Gate',
    'Shared Test Gate',
    'Future Azure / Entra Gate',
    'Pass',
    'Fail',
    'Decider',
    'Connection owners identified',
    'Service principals have Dataverse roles',
    'Passing this gate authorizes implementation planning'
) | Out-Null

Write-Host "Readiness contract validation passed. ($($adrFiles.Count) ADR files covered)"

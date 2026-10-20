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
        throw "Missing MVP activation artifact: $RelativePath"
    }

    $content = Get-Content -LiteralPath $fullPath -Raw
    if ([string]::IsNullOrWhiteSpace($content)) {
        throw "MVP activation artifact is empty: $RelativePath"
    }

    foreach ($pattern in $Patterns) {
        if ($content -notmatch [regex]::Escape($pattern)) {
            throw "$RelativePath does not reference required text: $pattern"
        }
    }

    return $content
}

Assert-FileContains -RelativePath 'docs/deployment/alm-activation.md' -Patterns @(
    'CareOps Decisions',
    'decisionsworkspace',
    'Dylan',
    'unmanaged solution',
    'New-DylanDeploymentEvidence.ps1',
    'No shared test deployment without connection ownership and rollback notes'
) | Out-Null

Assert-FileContains -RelativePath 'docs/deployment/release-artifacts.md' -Patterns @(
    'New-ReleaseArtifact.ps1',
    'Unpack-Solution.ps1',
    'Import-Solution.ps1',
    'Release Notes Template',
    'Operational Owner',
    'Rollback Documentation'
) | Out-Null

Assert-FileContains -RelativePath 'docs/deployment/solution-checker-ci.md' -Patterns @(
    'validate.yml',
    'solution checker',
    'SOLUTION_CHECKER_ENABLED',
    'Power Platform',
    'packed solution'
) | Out-Null

Assert-FileContains -RelativePath 'docs/deployment/sample-data.md' -Patterns @(
    'sample-governance-data.json',
    'Import-SeedData.ps1',
    'feature switchboard',
    'Teams',
    'Planner',
    'SharePoint',
    'AI Builder'
) | Out-Null

Assert-FileContains -RelativePath 'docs/deployment/environment-configuration-wizard.md' -Patterns @(
    'Environment Configuration Wizard',
    'Environment Variables',
    'Connection References',
    'Teams',
    'Planner',
    'SharePoint',
    'AI Builder',
    'Needs review'
) | Out-Null

Assert-FileContains -RelativePath 'docs/architecture/per-meeting-feature-switchboard.md' -Patterns @(
    'meeting series',
    'Meeting-level overrides',
    'environment gate',
    'Planner',
    'Teams',
    'SharePoint',
    'AI Builder',
    'Missing connector references should produce a clear disabled or pending state'
) | Out-Null

Assert-FileContains -RelativePath 'docs/architecture/ai-human-review-gates.md' -Patterns @(
    'Human review',
    'Publication approval',
    'dft_AIBuilderRun',
    'reviewer',
    'reviewed on',
    'No AI-generated content can reach published minutes, Teams posts, or decision/action registers without human review evidence'
) | Out-Null

Assert-FileContains -RelativePath 'docs/architecture/governance-reporting.md' -Patterns @(
    'Dataverse operational views',
    'Power Apps dashboards',
    'Power BI or Fabric',
    'governanceHierarchyBodyCount',
    'connectorHealthEventCount',
    'Governance configuration reporting'
) | Out-Null

Assert-FileContains -RelativePath 'docs/deployment/environment-readiness.md' -Patterns @(
    'Teams and Planner connector references are still missing',
    'Graph group-specific calls return 403',
    'SharePoint document libraries',
    'Teams file locations',
    'document registration',
    'Word/PDF export path',
    'live activation blocker'
) | Out-Null

Assert-FileContains -RelativePath 'docs/implementation/decisions-feature-coverage.md' -Patterns @(
    'Workspace home and launch navigation',
    'Meeting series and meetings',
    'Per-meeting configuration',
    'Medication Safety committee operating model',
    'All major Decisions-style features',
    'Teams/SharePoint/Planner wiring'
) | Out-Null

Assert-FileContains -RelativePath 'powerplatform/src/app/app-map.yaml' -Patterns @(
    'feature switchboard',
    'configuration',
    'SharePoint document store',
    'Integration Log',
    'Requires live AI Builder, Teams, Planner, or Outlook connector access for full verification'
) | Out-Null

Assert-FileContains -RelativePath 'powerplatform/src/app/commands.yaml' -Patterns @(
    'publish_minutes',
    'HumanReviewStatus',
    'Minutes require approval and human review before publishing.',
    'SharePoint document store',
    'feature switchboard'
) | Out-Null

Assert-FileContains -RelativePath 'tests/powerapps/DecisionsWorkspace.testplan.yaml' -Patterns @(
    'app-activation',
    'flow-activation',
    'import-validation',
    'configuration-wizard',
    'review-gate',
    'reporting'
) | Out-Null

Write-Host 'MVP activation contract validation passed.'

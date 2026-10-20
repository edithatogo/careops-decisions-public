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
        throw "Missing test automation artifact: $RelativePath"
    }

    $content = Get-Content -LiteralPath $fullPath -Raw
    if ([string]::IsNullOrWhiteSpace($content)) {
        throw "Test automation artifact is empty: $RelativePath"
    }

    foreach ($pattern in $Patterns) {
        if ($content -notmatch [regex]::Escape($pattern)) {
            throw "$RelativePath does not reference required text: $pattern"
        }
    }

    return $content
}

$workflowText = Assert-FileContains -RelativePath '.github/workflows/validate.yml' -Patterns @(
    'Invoke-RepoValidation.ps1 -RequireImplementationScaffolds',
    'New-ReleaseArtifact.ps1 -Unmanaged',
    'Upload packed solution artifact',
    'Run solution checker',
    'SOLUTION_CHECKER_ENABLED'
)

if ($workflowText -notmatch 'pull_request' -or $workflowText -notmatch 'workflow_dispatch') {
    throw 'validate.yml must support pull_request and workflow_dispatch validation.'
}

$testPlanText = Assert-FileContains -RelativePath 'tests/powerapps/DecisionsWorkspace.testplan.yaml' -Patterns @(
    'governance-body',
    'meeting-agenda',
    'minutes-decisions-actions',
    'integrations',
    'feature-switchboard',
    'app-activation',
    'flow-activation',
    'import-validation',
    'configuration-wizard',
    'review-gate',
    'reporting'
)

$suitePaths = [regex]::Matches($testPlanText, 'path:\s+(suites/[A-Za-z0-9_-]+\.yaml)') |
    ForEach-Object { $_.Groups[1].Value.Replace('/', '\') } |
    Sort-Object -Unique

if ($suitePaths.Count -lt 11) {
    throw "Expected at least 11 Power Apps suite paths; found $($suitePaths.Count)."
}

$allowedReadiness = 'local|requires-live-dylan|requires-live-connector'
foreach ($suitePath in $suitePaths) {
    $relativePath = "tests/powerapps/$($suitePath.Replace('\', '/'))"
    $suiteText = Assert-FileContains -RelativePath $relativePath -Patterns @(
        'name:',
        'status: scaffold',
        'readiness:',
        'tests:',
        '- id:'
    )

    if ($suiteText -notmatch "(?m)^readiness:\s+($allowedReadiness)\s*$") {
        throw "$relativePath has an invalid or missing top-level readiness tag."
    }
}

Assert-FileContains -RelativePath 'docs/testing/test-tagging-strategy.md' -Patterns @(
    'local',
    'requires-live-connector',
    'requires-live-dylan',
    'skipped with a CI-appropriate reason',
    'Connector Dependency Matrix',
    'governance-body',
    'integrations',
    'Planner',
    'Teams',
    'SharePoint',
    'AI Builder',
    'Graph'
) | Out-Null

Assert-FileContains -RelativePath 'docs/testing/acceptance-evidence-template.md' -Patterns @(
    'artifacts/deployment/',
    'Timestamp',
    'Environment',
    'Solution version',
    'Tester',
    'Profile name',
    'Scenario results',
    'PASS | FAIL | SKIP',
    'Skip reason'
) | Out-Null

Assert-FileContains -RelativePath 'docs/testing/smoke-runbook.md' -Patterns @(
    'CI Wrapper Strategy',
    'Repo validation first',
    'Solution pack',
    'Solution checker',
    'Evidence capture',
    'Connector-gated tests',
    'Invoke-SmokeRunbook.ps1 -DryRun'
) | Out-Null

$smokeScenarios = Assert-FileContains -RelativePath 'tests/smoke/smoke-scenarios.md' -Patterns @(
    'SMK-01',
    'SMK-17',
    'Planner',
    'Teams',
    'AI Builder',
    'Integration logs',
    'Feature switchboard'
)

foreach ($scenarioNumber in 1..17) {
    $scenarioId = 'SMK-{0:d2}' -f $scenarioNumber
    if ($smokeScenarios -notmatch [regex]::Escape($scenarioId)) {
        throw "Smoke scenarios are missing $scenarioId."
    }
}

Write-Host "Test automation contract validation passed. ($($suitePaths.Count) suite files covered)"

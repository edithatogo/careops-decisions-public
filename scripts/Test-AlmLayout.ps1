param(
    [switch]$Strict
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$requiredPaths = @(
    'product.md',
    'tech-stack.md',
    'workflow.md',
    'conductor/status.md',
    'docs/source-map.md',
    'docs/tooling-inventory.md',
    'docs/testing/test-strategy.md',
    'powerplatform/src/README.md',
    'powerplatform/solutions/README.md',
    'powerplatform/solutions/DecisionsWorkspace/README.md',
    'powerplatform/settings/README.md',
    'powerplatform/settings/deployment-settings.sample.json',
    'powerplatform/src/seed/sample-governance-data.json',
    'tests/smoke/README.md'
)

$optionalImplementationPaths = @(
    'powerplatform/src/schema/dataverse-tables.yaml',
    'powerplatform/src/app/app-map.yaml',
    'powerplatform/src/flows/flow-catalog.yaml',
    'tests/powerapps/DecisionsWorkspace.testplan.yaml'
)

$trackDirs = @(
    'track-00-conductor-tooling-inventory_20260502',
    'track-01-research-feature-map_20260502',
    'track-02-template-kit-evaluation_20260502',
    'track-03-power-platform-architecture_20260502',
    'track-04-alm-test-harness_20260502',
    'track-05-canvas-app-mvp_20260502',
    'track-06-automation-integrations_20260502',
    'track-07-future-azure-entra-upgrade_20260502',
    'track-08-microsoft-platform-integration_20260502',
    'track-09-governance-model_20260502',
    'track-10-meeting-records_20260502',
    'track-11-invites-attendees_20260502',
    'track-12-ai-transcripts-minutes_20260502'
)

$failures = New-Object System.Collections.Generic.List[string]

foreach ($path in $requiredPaths) {
    $fullPath = Join-Path $root $path
    if (-not (Test-Path -LiteralPath $fullPath)) {
        $failures.Add("Missing required path: $path")
    }
}

$solutionManifestCandidates = @(
    'powerplatform/solutions/DecisionsWorkspace/Other/Solution.xml',
    'powerplatform/solutions/DecisionsWorkspace/src/Other/Solution.xml',
    'powerplatform/solutions/DecisionsWorkspace/Solution.xml'
)
if (-not ($solutionManifestCandidates | Where-Object { Test-Path -LiteralPath (Join-Path $root $_) })) {
    $failures.Add("Missing required solution manifest: one of $($solutionManifestCandidates -join ', ')")
}

foreach ($track in $trackDirs) {
    foreach ($file in @('spec.md', 'plan.md', 'metadata.json')) {
        $relativePath = Join-Path "conductor/tracks/$track" $file
        $fullPath = Join-Path $root $relativePath
        if (-not (Test-Path -LiteralPath $fullPath)) {
            $failures.Add("Missing track file: $relativePath")
        }
    }
}

foreach ($commandName in @('pac', 'pacx', 'git', 'node', 'npm', 'dotnet')) {
    if (-not (Get-Command $commandName -ErrorAction SilentlyContinue)) {
        $message = "Tool not found on PATH: $commandName"
        if ($Strict) {
            $failures.Add($message)
        } else {
            Write-Warning $message
        }
    }
}

foreach ($path in $optionalImplementationPaths) {
    $fullPath = Join-Path $root $path
    if (-not (Test-Path -LiteralPath $fullPath)) {
        Write-Warning "Implementation scaffold not present yet: $path"
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "ALM layout validation passed."

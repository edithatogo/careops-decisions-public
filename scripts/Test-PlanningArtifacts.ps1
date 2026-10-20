param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot

function Assert-FilePresent {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing planning artifact: $RelativePath"
    }

    $content = Get-Content -LiteralPath $path -Raw
    if ([string]::IsNullOrWhiteSpace($content)) {
        throw "Planning artifact is empty: $RelativePath"
    }

    Write-Host "Present planning artifact: $RelativePath"
}

function Assert-TextContains {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath,
        [Parameter(Mandatory = $true)]
        [string[]]$Patterns
    )

    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing planning artifact for content check: $RelativePath"
    }

    $content = Get-Content -LiteralPath $path -Raw
    foreach ($pattern in $Patterns) {
        if ($content -notmatch [regex]::Escape($pattern)) {
            throw "Planning artifact $RelativePath does not reference required text: $pattern"
        }
    }
}

$trackIds = 14..20 | ForEach-Object {
    switch ($_) {
        14 { 'track-14-alm-solution-release_20260502' }
        15 { 'track-15-security-dlp-retention_20260502' }
        16 { 'track-16-governance-reporting_20260502' }
        17 { 'track-17-operations-monitoring-support_20260502' }
        18 { 'track-18-data-migration-bootstrap_20260502' }
        19 { 'track-19-test-automation-ci_20260502' }
        20 { 'track-20-decisions-readiness-gates_20260502' }
    }
}

$trackIds += 'track-21-alm-activation-ci_20260502'
$trackIds += 'track-23-medication-safety-operationalization_20260504'
$trackIds += 'track-24-agent-workflow-operations_20260504'

foreach ($trackId in $trackIds) {
    Assert-FilePresent -RelativePath "conductor/tracks/$trackId/metadata.json"
    Assert-FilePresent -RelativePath "conductor/tracks/$trackId/spec.md"
    Assert-FilePresent -RelativePath "conductor/tracks/$trackId/plan.md"

    $metadataPath = Join-Path $root "conductor/tracks/$trackId/metadata.json"
    $metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
    if ($metadata.id -ne $trackId) {
        throw "Track metadata id mismatch for $trackId"
    }
    if (-not $metadata.parallelizable) {
        throw "Track should be parallelizable: $trackId"
    }
}

$adrFiles = @(
    'docs/adr/README.md',
    'docs/adr/0001-dataverse-system-of-record.md',
    'docs/adr/0002-alm-first-delivery.md',
    'docs/adr/0003-model-driven-plus-teams-canvas.md',
    'docs/adr/0004-ai-human-review-gate.md',
    'docs/adr/0005-graph-azure-entra-deferred.md',
    'docs/adr/0006-power-bi-deferred-until-stable-data.md'
)

foreach ($adrFile in $adrFiles) {
    Assert-FilePresent -RelativePath $adrFile
}

$readinessFiles = @(
    'docs/readiness/licensing-capacity-matrix.md',
    'docs/readiness/tenant-permissions-matrix.md',
    'docs/readiness/service-account-strategy.md',
    'docs/readiness/implementation-readiness-gates.md',
    'docs/readiness/skill-agent-tool-matrix.md',
    'docs/operations/README.md',
    'docs/operations/agent-runbook.md',
    'docs/guides/decisions-workspace-quick-start.md',
    'docs/implementation/decisions-feature-coverage.md',
    'docs/architecture/ai-builder-minutes-pattern.md',
    'docs/operations/medication-safety-committee-workflows.md'
)

foreach ($readinessFile in $readinessFiles) {
    Assert-FilePresent -RelativePath $readinessFile
}

Assert-FilePresent -RelativePath 'scripts/Verify-DylanDeployment.ps1'

Assert-TextContains -RelativePath 'conductor/tracks/README.md' -Patterns $trackIds
Assert-TextContains -RelativePath 'conductor/status.md' -Patterns $trackIds
Assert-TextContains -RelativePath 'docs/implementation-roadmap.md' -Patterns @('Track 14', 'Track 15', 'Track 16', 'Track 17', 'Track 18', 'Track 19', 'Track 20')
Assert-TextContains -RelativePath 'docs/source-map.md' -Patterns @('docs/adr/README.md', 'docs/readiness/licensing-capacity-matrix.md', 'docs/readiness/skill-agent-tool-matrix.md')
Assert-TextContains -RelativePath 'docs/source-map.md' -Patterns @('docs/operations/agent-runbook.md', 'scripts/Verify-DylanDeployment.ps1')
Assert-TextContains -RelativePath 'docs/source-map.md' -Patterns @(
    'docs/implementation/decisions-feature-coverage.md',
    'docs/guides/decisions-workspace-quick-start.md',
    'docs/architecture/ai-builder-minutes-pattern.md',
    'docs/operations/medication-safety-committee-workflows.md'
)
Assert-TextContains -RelativePath 'docs/source-map.md' -Patterns @(
    'docs/deployment/deployment-profiles.md',
    'docs/deployment/restricted-runtime-matrix.md'
)
Assert-TextContains -RelativePath 'docs/implementation/decisions-feature-coverage.md' -Patterns @(
    'Meeting series and meetings',
    'AI minutes',
    'Per-meeting configuration',
    'Built But Not Fully Used Yet'
)
Assert-TextContains -RelativePath 'docs/deployment/restricted-runtime-matrix.md' -Patterns @(
    'Capability Matrix',
    'What you can and cannot do in restricted mode',
    'Local / restricted mode',
    'Connector-enabled mode'
)
Assert-TextContains -RelativePath 'docs/architecture/ai-builder-minutes-pattern.md' -Patterns @(
    'AI output remains draft until reviewed',
    'Prompt name and version',
    'decisions_EnableAiBuilderMinutes'
)
Assert-TextContains -RelativePath 'docs/operations/medication-safety-committee-workflows.md' -Patterns @(
    'Gmail is not a valid source',
    'Planner sync',
    'Teams summary'
)
Assert-TextContains -RelativePath 'docs/operations/agent-runbook.md' -Patterns @(
    'Parallelize source work; serialize live Dylan writes',
    'Writer',
    'Reviewer',
    'operator@example.invalid',
    'Gmail is not a valid source'
)
Assert-TextContains -RelativePath 'docs/implementation-roadmap.md' -Patterns @(
    'Track 24: agent workflow operations, writer/reviewer pairing, live deployment serialization, and Dylan verification.'
)
Assert-TextContains -RelativePath 'docs/readiness/skill-agent-tool-matrix.md' -Patterns @('pac', 'pacx', 'XrmToolBox', 'Power BI', 'entra-app-registration')
Assert-TextContains -RelativePath 'tests/smoke/smoke-scenarios.md' -Patterns @(
    'create governance body',
    'schedule meeting',
    'add agenda and papers',
    'generate minutes draft',
    'approve minutes',
    'record decisions',
    'record actions',
    'optionally sync Planner'
)
Assert-TextContains -RelativePath 'tests/smoke/seed-data-checklist.md' -Patterns @('SMK-14', 'SMK-15')
Assert-TextContains -RelativePath 'docs/testing/smoke-runbook.md' -Patterns @(
    'without writing to Power Platform',
    'Local smoke runs can skip those scenarios with a recorded reason',
    'Record outcomes for each scenario'
)
Assert-TextContains -RelativePath 'docs/testing/implementation-readiness.md' -Patterns @(
    'Local readiness checks only cover source artifacts',
    'connector placeholders are deployment blockers',
    'not build blockers'
)

Assert-TextContains -RelativePath 'conductor/tracks/track-24-agent-workflow-operations_20260504/spec.md' -Patterns @(
    'reviewer pairings',
    'live-deployment rules',
    'repeatable Dylan verification script',
    'writer/reviewer pairs',
    'serialized through one deployment operator',
    'optional Medication Safety sample records',
    'real NSW committee data gated'
)

Assert-TextContains -RelativePath 'conductor/tracks/track-24-agent-workflow-operations_20260504/plan.md' -Patterns @(
    'Support JSON output for CI or handoff logs.',
    'Keep the script free of tokens and live write operations.',
    'Add Track 24 references to the track index, Conductor status, roadmap, source map, and planning validation.'
)

Assert-TextContains -RelativePath 'scripts/Verify-DylanDeployment.ps1' -Patterns @(
    'PacTimeoutSeconds',
    'TimeoutSeconds',
    'WaitForExit',
    'Timed out after',
    'ConvertTo-Json',
    'ExitCode = 124',
    'Get-Command pac'
)

Write-Host "Planning artifact validation completed."

param()

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$failures = New-Object System.Collections.Generic.List[string]

function Get-ArtifactText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $path = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing feature switchboard artifact: $RelativePath")
        return ''
    }

    $content = Get-Content -LiteralPath $path -Raw
    if ([string]::IsNullOrWhiteSpace($content)) {
        $failures.Add("Empty feature switchboard artifact: $RelativePath")
        return ''
    }

    return $content
}

function Assert-ContainsText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath,
        [Parameter(Mandatory = $true)]
        [string]$Content,
        [Parameter(Mandatory = $true)]
        [string[]]$Patterns
    )

    foreach ($pattern in $Patterns) {
        if ($Content -notmatch [regex]::Escape($pattern)) {
            $failures.Add("$RelativePath is missing required switchboard text: $pattern")
        }
    }
}

function Assert-ContainsRegex {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath,
        [Parameter(Mandatory = $true)]
        [string]$Content,
        [Parameter(Mandatory = $true)]
        [string]$Pattern,
        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    if ($Content -notmatch $Pattern) {
        $failures.Add("$RelativePath is missing required switchboard contract: $Description")
    }
}

$switchboardDoc = Get-ArtifactText -RelativePath 'docs/architecture/per-meeting-feature-switchboard.md'
$schemaReadme = Get-ArtifactText -RelativePath 'powerplatform/src/schema/README.md'
$appMap = Get-ArtifactText -RelativePath 'powerplatform/src/app/app-map.yaml'
$aiFlow = Get-ArtifactText -RelativePath 'powerplatform/src/flows/ai-transcript-minutes.yaml'
$plannerFlow = Get-ArtifactText -RelativePath 'powerplatform/src/flows/planner-sync.yaml'
$teamsFlow = Get-ArtifactText -RelativePath 'powerplatform/src/flows/teams-summary-post.yaml'
$outlookFlow = Get-ArtifactText -RelativePath 'powerplatform/src/flows/outlook-invite-sync.yaml'
$seedReadme = Get-ArtifactText -RelativePath 'powerplatform/src/seed/README.md'
$sampleDataDoc = Get-ArtifactText -RelativePath 'docs/deployment/sample-data.md'
$featureCoverage = Get-ArtifactText -RelativePath 'docs/implementation/decisions-feature-coverage.md'
$reportingReadme = Get-ArtifactText -RelativePath 'powerplatform/src/reporting/README.md'
$governanceReporting = Get-ArtifactText -RelativePath 'docs/architecture/governance-reporting.md'
$testPlan = Get-ArtifactText -RelativePath 'tests/powerapps/DecisionsWorkspace.testplan.yaml'
$featureSuite = Get-ArtifactText -RelativePath 'tests/powerapps/suites/feature-switchboard.yaml'
$smokeScenarios = Get-ArtifactText -RelativePath 'tests/smoke/smoke-scenarios.md'
$seedChecklist = Get-ArtifactText -RelativePath 'tests/smoke/seed-data-checklist.md'

Assert-ContainsText -RelativePath 'docs/architecture/per-meeting-feature-switchboard.md' -Content $switchboardDoc -Patterns @(
    'feature definitions',
    'overrides',
    'meeting-level overrides',
    'disabled or pending state',
    'effective switchboard'
)

Assert-ContainsText -RelativePath 'powerplatform/src/schema/README.md' -Content $schemaReadme -Patterns @(
    'feature definition and override fields',
    'per-meeting feature switchboard',
    'schema contract'
)

Assert-ContainsText -RelativePath 'powerplatform/src/app/app-map.yaml' -Content $appMap -Patterns @(
    'feature_flags:',
    'transcript_import_enabled: true',
    'ai_builder_review_enabled: true',
    'planner_sync_enabled: false',
    'teams_post_enabled: false',
    'sharepoint_document_store_enabled: false',
    'full_dataverse_ready: false'
)

Assert-ContainsRegex -RelativePath 'powerplatform/src/flows/ai-transcript-minutes.yaml' -Content $aiFlow -Pattern '(?ms)field:\s+decisions_EnableAiBuilderMinutes' -Description 'AI flow reads the feature flag'
Assert-ContainsText -RelativePath 'powerplatform/src/flows/ai-transcript-minutes.yaml' -Content $aiFlow -Patterns @(
    'check-feature-flag',
    'log-disabled',
    'status: Skipped',
    'AI Builder minutes generation is disabled.'
)

foreach ($flowPath in @(
    'powerplatform/src/flows/planner-sync.yaml',
    'powerplatform/src/flows/teams-summary-post.yaml',
    'powerplatform/src/flows/outlook-invite-sync.yaml'
)) {
    $flowContent = switch ($flowPath) {
        'powerplatform/src/flows/planner-sync.yaml' { $plannerFlow }
        'powerplatform/src/flows/teams-summary-post.yaml' { $teamsFlow }
        default { $outlookFlow }
    }

    Assert-ContainsText -RelativePath $flowPath -Content $flowContent -Patterns @(
        'log-skip',
        'status: Skipped'
    )
}

Assert-ContainsText -RelativePath 'powerplatform/src/seed/README.md' -Content $seedReadme -Patterns @(
    'feature definitions',
    'meeting-level overrides',
    'disabled features',
    'integration binding placeholders'
)

Assert-ContainsText -RelativePath 'docs/deployment/sample-data.md' -Content $sampleDataDoc -Patterns @(
    'feature definitions',
    'overrides',
    'binding placeholders',
    'AI Builder'
)

Assert-ContainsText -RelativePath 'docs/implementation/decisions-feature-coverage.md' -Content $featureCoverage -Patterns @(
    'Per-meeting configuration',
    'Reporting and governance audit',
    'future Power BI/Fabric model'
)

Assert-ContainsText -RelativePath 'powerplatform/src/reporting/README.md' -Content $reportingReadme -Patterns @(
    'Dataverse-first reporting surfaces',
    'Power BI and Fabric stay deferred',
    'Access Principles'
)

Assert-ContainsText -RelativePath 'docs/architecture/governance-reporting.md' -Content $governanceReporting -Patterns @(
    'Dataverse operational views',
    'Power Apps dashboards',
    'Power BI or Fabric',
    'Reporting is included in deployment validation and smoke testing.'
)

Assert-ContainsText -RelativePath 'tests/powerapps/DecisionsWorkspace.testplan.yaml' -Content $testPlan -Patterns @(
    'feature-switchboard',
    'per-meeting feature switchboard',
    'integration placeholders'
)

Assert-ContainsText -RelativePath 'tests/powerapps/suites/feature-switchboard.yaml' -Content $featureSuite -Patterns @(
    'feature definitions',
    'meeting-level override',
    'disabled capability',
    'skip reason',
    'effective feature state'
)

Assert-ContainsText -RelativePath 'tests/smoke/smoke-scenarios.md' -Content $smokeScenarios -Patterns @(
    'Per-meeting feature switchboard',
    'feature definitions',
    'meeting-level override',
    'skipped or suppressed'
)

Assert-ContainsText -RelativePath 'tests/smoke/seed-data-checklist.md' -Content $seedChecklist -Patterns @(
    'SMK-17',
    'Per-meeting feature switchboard',
    'Connector skips or suppression reasons documented where needed'
)

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'Feature switchboard contract validation passed.'

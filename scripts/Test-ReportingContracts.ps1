[CmdletBinding()]
param(
    [switch]$DryRun,
    [switch]$PassThru
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$failures = New-Object System.Collections.Generic.List[string]
$warnings = New-Object System.Collections.Generic.List[string]

function Add-Failure {
    param([string]$Message)
    $failures.Add($Message)
}

function Add-Warning {
    param([string]$Message)
    $warnings.Add($Message)
}

function Get-ArtifactText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $path = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        Add-Failure "Missing reporting contract artifact: $RelativePath"
        return ''
    }

    $content = Get-Content -LiteralPath $path -Raw
    if ([string]::IsNullOrWhiteSpace($content)) {
        $failures.Add("Empty reporting contract artifact: $RelativePath")
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
            $failures.Add("$RelativePath is missing required reporting text: $pattern")
        }
    }
}

$reportingContractPath = 'powerplatform/src/reporting/dataverse-dashboards.yaml'
$reportingReadmePath = 'powerplatform/src/reporting/README.md'
$governanceReportingPath = 'docs/architecture/governance-reporting.md'
$featureCoveragePath = 'docs/implementation/decisions-feature-coverage.md'
$smokeScenariosPath = 'tests/smoke/smoke-scenarios.md'
$seedChecklistPath = 'tests/smoke/seed-data-checklist.md'

$reportingContract = Get-ArtifactText -RelativePath $reportingContractPath
$reportingReadme = Get-ArtifactText -RelativePath $reportingReadmePath
$governanceReporting = Get-ArtifactText -RelativePath $governanceReportingPath
$featureCoverage = Get-ArtifactText -RelativePath $featureCoveragePath
$smokeScenarios = Get-ArtifactText -RelativePath $smokeScenariosPath
$seedChecklist = Get-ArtifactText -RelativePath $seedChecklistPath

Assert-ContainsText -RelativePath $reportingContractPath -Content $reportingContract -Patterns @(
    'schema: decisions.workspace.reporting.v1',
    'implementationLayer: Dataverse operational views and app dashboards',
    'Power BI semantic model',
    'Fabric lakehouse or shortcuts',
    'Graph-backed enterprise analytics',
    'secretary-workbench',
    'chair-review',
    'member-actions',
    'governance-audit',
    'governance-configuration-health',
    'meeting-pipeline-next-30-days',
    'upcoming-meetings-missing-agenda',
    'draft-minutes-pending-review',
    'minutes-awaiting-chair-approval',
    'unapproved-minutes-with-publish-url',
    'papers-missing-or-late',
    'decision-register-open',
    'decisions-overdue-review',
    'decisions-assigned-to-me',
    'my-open-actions',
    'my-overdue-actions',
    'overdue-actions',
    'actions-synced-without-planner-task-id',
    'access-exceptions-active',
    'integration-errors-open',
    'stale-integration-status',
    'ai-review-queue',
    'records-missing-owner',
    'overdue-review-register',
    'governance-hierarchy-overview',
    'feature-switch-defaults',
    'feature-switch-templates',
    'meeting-feature-overrides',
    'connector-health-summary'
)

Assert-ContainsText -RelativePath $reportingContractPath -Content $reportingContract -Patterns @(
    'meetingsScheduledNext30Days',
    'minutesPendingReviewCount',
    'minutesAwaitingChairApproval',
    'unapprovedMinutesWithPublishUrlCount',
    'latePaperCount',
    'openDecisionCount',
    'decisionsOverdueReviewCount',
    'overdueActionCount',
    'syncedActionsMissingPlannerTaskId',
    'activeAccessExceptionCount',
    'openIntegrationErrorCount',
    'staleIntegrationStatusCount',
    'aiRunsPendingReview',
    'recordsMissingOwnerCount',
    'overdueReviewCount',
    'governanceHierarchyBodyCount',
    'featureSwitchTemplateCount',
    'meetingFeatureOverrideCount',
    'connectorHealthEventCount'
)

Assert-ContainsText -RelativePath $reportingContractPath -Content $reportingContract -Patterns @(
    'missing-owners',
    'overdue-reviews',
    'stale-integration-status',
    'unapproved-minutes-with-publish-url',
    'actions-synced-without-planner-task-id'
)

Assert-ContainsText -RelativePath $reportingReadmePath -Content $reportingReadme -Patterns @(
    'Dataverse-first reporting surfaces',
    'MVP Reporting Order',
    'Power BI/Fabric semantic model',
    'MVP Dashboard Coverage',
    'Power BI and Fabric stay deferred'
)

Assert-ContainsText -RelativePath $governanceReportingPath -Content $governanceReporting -Patterns @(
    'Dataverse operational views',
    'Power Apps dashboards',
    'Power BI or Fabric',
    'Reporting is included in deployment validation and smoke testing.',
    'Source Artifacts',
    'MVP Activation Tasks',
    'governanceHierarchyBodyCount',
    'featureSwitchTemplateCount',
    'meetingFeatureOverrideCount',
    'connectorHealthEventCount',
    'Governance configuration reporting must show hierarchy, feature-switch adoption, connector health, access exceptions, and review gates'
)

Assert-ContainsText -RelativePath $featureCoveragePath -Content $featureCoverage -Patterns @(
    'Reporting and governance audit',
    'Dataverse dashboard YAML',
    'future Power BI/Fabric model',
    'Per-meeting configuration',
    'feature definitions, series defaults, meeting overrides'
)

Assert-ContainsText -RelativePath $smokeScenariosPath -Content $smokeScenarios -Patterns @(
    'SMK-15',
    'Governance dashboards',
    'SMK-16',
    'Reporting data-quality checks',
    'SMK-17',
    'Per-meeting feature switchboard',
    'without requiring Power BI/Fabric or live Planner/Teams/SharePoint connectors'
)

Assert-ContainsText -RelativePath $seedChecklistPath -Content $seedChecklist -Patterns @(
    'SMK-15',
    'SMK-16',
    'SMK-17',
    'Connector skips or suppression reasons documented where needed'
)

if ($PassThru) {
    return [pscustomobject]@{
        Passed     = ($failures.Count -eq 0)
        Failures   = $failures.ToArray()
        Warnings   = $warnings.ToArray()
        Summary    = if ($failures.Count -eq 0) { 'Reporting contract validation passed.' } else { 'Reporting contract validation failed.' }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Add-Failure $_ }
    Write-Host 'FAIL: Reporting contract validation failed.'
    exit 1
}

if ($warnings.Count -gt 0) {
    $warnings | ForEach-Object { Write-Warning $_ }
}

Write-Host 'PASS: Reporting contract validation passed.'

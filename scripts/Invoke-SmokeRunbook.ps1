param(
    [string]$SeedPath = (Join-Path $PSScriptRoot '..\powerplatform\src\seed\sample-governance-data.json'),
    [string]$ScenariosPath = (Join-Path $PSScriptRoot '..\tests\smoke\smoke-scenarios.md'),
    [string[]]$RequiredPaths,
    [switch]$DryRun,
    [switch]$ActivationValidation,
    [switch]$ReviewGateValidation,
    [switch]$ReportingValidation,
    [string]$TestEngineSuitePath = (Join-Path $PSScriptRoot '..\tests\powerapps\suites')
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
function Resolve-RepoOrAbsolutePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

$resolvedSeedPath = Resolve-RepoOrAbsolutePath -Path $SeedPath
$resolvedScenariosPath = Resolve-RepoOrAbsolutePath -Path $ScenariosPath

if (-not $RequiredPaths -or $RequiredPaths.Count -eq 0) {
    $RequiredPaths = @(
        'tests/smoke/runbook.md',
        'tests/smoke/seed-data-checklist.md',
        'tests/smoke/smoke-scenarios.md',
        'tests/smoke/README.md',
        'docs/testing/smoke-runbook.md',
        'docs/testing/test-strategy.md',
        'powerplatform/src/seed/sample-governance-data.json',
        'tests/powerapps/DecisionsWorkspace.testplan.yaml',
        'tests/powerapps/suites/app-activation.yaml',
        'tests/powerapps/suites/flow-activation.yaml',
        'tests/powerapps/suites/import-validation.yaml',
        'tests/powerapps/suites/configuration-wizard.yaml',
        'tests/powerapps/suites/review-gate.yaml',
        'tests/powerapps/suites/reporting.yaml'
    )
}

function Resolve-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $RelativePath))
}

function Test-RequiredPaths {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$RelativePaths
    )

    $missing = New-Object System.Collections.Generic.List[string]
    foreach ($path in $RelativePaths) {
        $fullPath = Resolve-RepoPath -RelativePath $path
        if (-not (Test-Path -LiteralPath $fullPath)) {
            $missing.Add($path)
        }
    }

    return $missing
}

function Get-SmokeScenarios {
    param(
        [Parameter(Mandatory = $true)]
        [string]$MarkdownPath
    )

    $lines = Get-Content -LiteralPath $MarkdownPath
    $scenarios = New-Object System.Collections.Generic.List[object]

    foreach ($line in $lines) {
        if ($line -notmatch '^\|\s*(SMK-\d+)\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|$') {
            continue
        }

        $scenarios.Add([pscustomobject]@{
            Id = $Matches[1]
            Name = $Matches[2].Trim()
            Steps = $Matches[3].Trim()
            ExpectedResult = $Matches[4].Trim()
        })
    }

    return $scenarios
}

function Get-SeedSummary {
    param(
        [Parameter(Mandatory = $true)]
        [string]$JsonPath
    )

    $seed = Get-Content -LiteralPath $JsonPath -Raw | ConvertFrom-Json

    $summary = [ordered]@{
        GovernanceBodies = @($seed.governanceBodies).Count
        Committees = @($seed.committees).Count
        TermsOfReference = @($seed.termsOfReference).Count
        Members = @($seed.members).Count
        MeetingSeries = @($seed.meetingSeries).Count
        Meetings = @($seed.meetings).Count
        Attendees = @($seed.attendees).Count
        AgendaItems = @($seed.agendaItems).Count
        Transcripts = @($seed.transcripts).Count
        AIBuilderPrompts = @($seed.aiBuilderPrompts).Count
        AIBuilderRuns = @($seed.aiBuilderRuns).Count
        Decisions = @($seed.decisions).Count
        Actions = @($seed.actions).Count
        GovernanceBodyNames = @($seed.governanceBodies | ForEach-Object { $_.name })
        CommitteeNames = @($seed.committees | ForEach-Object { $_.name })
        MeetingTitles = @($seed.meetings | ForEach-Object { $_.title })
    }

    return [pscustomobject]$summary
}

function New-ChecklistLine {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text,
        [switch]$Done
    )

    if ($Done) {
        return "[x] $Text"
    }

    return "[ ] $Text"
}

$missingPaths = Test-RequiredPaths -RelativePaths $RequiredPaths
$scenarios = Get-SmokeScenarios -MarkdownPath $resolvedScenariosPath
$seedSummary = Get-SeedSummary -JsonPath $resolvedSeedPath

Write-Host "Smoke runbook checklist"
Write-Host "Repository root: $repoRoot"
Write-Host "Seed file: $resolvedSeedPath"
Write-Host "Scenario file: $resolvedScenariosPath"
Write-Host ""

Write-Host "Scenario list"
foreach ($scenario in $scenarios) {
    Write-Host ("- {0} {1}" -f $scenario.Id, $scenario.Name)
}

Write-Host ""
Write-Host "Seed summary"
Write-Host ("- Governance bodies: {0}" -f $seedSummary.GovernanceBodies)
Write-Host ("- Committees: {0}" -f $seedSummary.Committees)
Write-Host ("- Terms of reference: {0}" -f $seedSummary.TermsOfReference)
Write-Host ("- Members: {0}" -f $seedSummary.Members)
Write-Host ("- Meeting series: {0}" -f $seedSummary.MeetingSeries)
Write-Host ("- Meetings: {0}" -f $seedSummary.Meetings)
Write-Host ("- Attendees: {0}" -f $seedSummary.Attendees)
Write-Host ("- Agenda items: {0}" -f $seedSummary.AgendaItems)
Write-Host ("- Transcripts: {0}" -f $seedSummary.Transcripts)
Write-Host ("- AI Builder prompts: {0}" -f $seedSummary.AIBuilderPrompts)
Write-Host ("- AI Builder runs: {0}" -f $seedSummary.AIBuilderRuns)
Write-Host ("- Decisions: {0}" -f $seedSummary.Decisions)
Write-Host ("- Actions: {0}" -f $seedSummary.Actions)

if ($ActivationValidation -or ($PSBoundParameters.Keys.Count -eq 0)) {
    Write-Host ""
    Write-Host "Activation validation"
    $foundAppSuite = Test-Path -LiteralPath (Resolve-RepoPath -RelativePath 'tests/powerapps/suites/app-activation.yaml')
    $foundFlowSuite = Test-Path -LiteralPath (Resolve-RepoPath -RelativePath 'tests/powerapps/suites/flow-activation.yaml')
    $foundImportSuite = Test-Path -LiteralPath (Resolve-RepoPath -RelativePath 'tests/powerapps/suites/import-validation.yaml')
    Write-Host ("{0} App activation suite (app-activation.yaml)          ACT-01 through ACT-04" -f $(if ($foundAppSuite) { "[x]" } else { "[ ]" }))
    Write-Host ("{0} Flow activation suite (flow-activation.yaml)        FLO-01 through FLO-04" -f $(if ($foundFlowSuite) { "[x]" } else { "[ ]" }))
    Write-Host ("{0} Import validation suite (import-validation.yaml)    IMP-01 through IMP-06" -f $(if ($foundImportSuite) { "[x]" } else { "[ ]" }))
    Write-Host ("{0} Configuration wizard suite (configuration-wizard.yaml)  CFG-01 through CFG-03" -f $(if (Test-Path -LiteralPath (Resolve-RepoPath -RelativePath 'tests/powerapps/suites/configuration-wizard.yaml')) { "[x]" } else { "[ ]" }))
}

if ($ReviewGateValidation -or ($PSBoundParameters.Keys.Count -eq 0)) {
    Write-Host ""
    Write-Host "Review gate validation"
    $foundReviewSuite = Test-Path -LiteralPath (Resolve-RepoPath -RelativePath 'tests/powerapps/suites/review-gate.yaml')
    Write-Host ("{0} Review gate suite (review-gate.yaml)               RVW-01 through RVW-05" -f $(if ($foundReviewSuite) { "[x]" } else { "[ ]" }))
    Write-Host "[ ] Draft minutes can be submitted for review (status -> in-review)"
    Write-Host "[ ] Reviewer can approve minutes (reviewer, timestamp, approval recorded)"
    Write-Host "[ ] Reviewer can reject minutes (returned to draft with notes)"
    Write-Host "[ ] AI-generated minutes are blocked from publish without review decision"
    Write-Host "[ ] Review audit trail captures reviewer identity, timestamp, and notes"
}

if ($ReportingValidation -or ($PSBoundParameters.Keys.Count -eq 0)) {
    Write-Host ""
    Write-Host "Reporting validation"
    $foundReportSuite = Test-Path -LiteralPath (Resolve-RepoPath -RelativePath 'tests/powerapps/suites/reporting.yaml')
    Write-Host ("{0} Reporting suite (reporting.yaml)                   RPT-01 through RPT-05" -f $(if ($foundReportSuite) { "[x]" } else { "[ ]" }))
    Write-Host "[ ] Governance dashboard views are populated from seed data"
    Write-Host "[ ] Decision register contains seeded decision record"
    Write-Host "[ ] Overdue actions view is populated"
    Write-Host "[ ] Integration error log view surfaces seeded error records"
    Write-Host "[ ] AI review queue shows records awaiting human review"
    Write-Host "[ ] Data-quality exceptions surface missing owners, overdue reviews, stale integrations"
    Write-Host "[ ] Switchboard metrics distinguish inherited defaults from meeting-level overrides"
}

Write-Host ""
Write-Host "Run checklist"
$checklist = New-Object System.Collections.Generic.List[string]
$checklist.Add((New-ChecklistLine -Text 'Confirm the target environment, solution version, tester, and run timestamp are recorded.' ))
$checklist.Add((New-ChecklistLine -Text 'Verify required files are present before touching the environment.' -Done:($missingPaths.Count -eq 0)))
$checklist.Add((New-ChecklistLine -Text 'Review seed counts and the seeded body, committee, meeting, agenda, decision, and action records.' -Done))
$checklist.Add((New-ChecklistLine -Text 'Run activation validation: verify app load, screen navigation, and baseline smoke (ACT-01 through ACT-04).' ))
$checklist.Add((New-ChecklistLine -Text 'Run flow contract validation: triggers, environment variables, and connector references (FLO-01 through FLO-04).' ))
$checklist.Add((New-ChecklistLine -Text 'Run import validation: seed record counts, relationship integrity, and switchboard data (IMP-01 through IMP-06).' ))
$checklist.Add((New-ChecklistLine -Text 'Run configuration wizard validation: field presence and value persistence (CFG-01 through CFG-03).' ))
$checklist.Add((New-ChecklistLine -Text 'Run review gate validation: draft-to-review transition, approve/reject, publish block (RVW-01 through RVW-05).' ))
$checklist.Add((New-ChecklistLine -Text 'Run reporting validation: dashboard views, data-quality exceptions, switchboard metrics (RPT-01 through RPT-05).' ))
$checklist.Add((New-ChecklistLine -Text 'Walk through each smoke scenario (SMK-01 through SMK-17) in order, using seed records as starting state.' ))
$checklist.Add((New-ChecklistLine -Text 'Capture evidence for each scenario, including screenshots or record links where useful.' ))
$checklist.Add((New-ChecklistLine -Text 'Log any connector-specific skips or environment gaps instead of retrying into a dead dependency.' ))
$checklist.Add((New-ChecklistLine -Text 'Mark each test and scenario as local or requires-live-dylan based on readiness annotations.' ))
$checklist.Add((New-ChecklistLine -Text 'Summarize pass, fail, skip, and follow-up items at the end of the run.' ))

foreach ($line in $checklist) {
    Write-Host $line
}

if ($missingPaths.Count -gt 0) {
    Write-Host ""
    Write-Warning "Missing required files:"
    foreach ($path in $missingPaths) {
        Write-Warning " - $path"
    }
}

if ($DryRun) {
    Write-Host ""
    Write-Host "Dry run only. No Power Platform writes were attempted."
}

if ($missingPaths.Count -gt 0) {
    exit 1
}

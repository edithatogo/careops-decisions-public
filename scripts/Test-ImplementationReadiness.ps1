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
        throw "Missing required artifact: $RelativePath"
    }

    $content = Get-Content -LiteralPath $path -Raw
    if ([string]::IsNullOrWhiteSpace($content)) {
        throw "Artifact is empty: $RelativePath"
    }

    Write-Host "Present artifact: $RelativePath"
}

function Assert-TextContains {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath,
        [Parameter(Mandatory = $true)]
        [string[]]$Patterns
    )

    $content = Get-Content -LiteralPath (Join-Path $root $RelativePath) -Raw
    foreach ($pattern in $Patterns) {
        if ($content -notmatch [regex]::Escape($pattern)) {
            throw "Artifact $RelativePath does not reference required text: $pattern"
        }
    }
}

function Get-JsonContent {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    Get-Content -LiteralPath (Join-Path $root $RelativePath) -Raw | ConvertFrom-Json
}

$requiredArtifacts = @(
    'powerplatform/src/app/app-map.yaml',
    'powerplatform/src/app/screens.md',
    'powerplatform/src/app/access-matrix.yaml',
    'powerplatform/src/app/commands.yaml',
    'powerplatform/src/app/components.yaml',
    'powerplatform/src/app/powerfx-patterns.md',
    'powerplatform/src/schema/dataverse-tables.yaml',
    'powerplatform/src/schema/relationships.yaml',
    'powerplatform/src/schema/choices.yaml',
    'powerplatform/src/schema/schema-build-plan.json',
    'powerplatform/src/flows/flow-catalog.yaml',
    'powerplatform/src/flows/planner-sync.yaml',
    'powerplatform/src/flows/outlook-invite-sync.yaml',
    'powerplatform/src/flows/teams-summary-post.yaml',
    'powerplatform/src/flows/org-hierarchy-sync.yaml',
    'powerplatform/src/flows/ai-transcript-minutes.yaml',
    'powerplatform/src/flow-contracts.md',
    'powerplatform/src/seed/sample-governance-data.json',
    'powerplatform/src/seed/README.md'
)

foreach ($artifact in $requiredArtifacts) {
    Assert-FilePresent -RelativePath $artifact
}

Assert-TextContains -RelativePath 'powerplatform/src/app/app-map.yaml' -Patterns @(
    'dft_GovernanceBody',
    'dft_TermsOfReference',
    'dft_CommitteeMembership',
    'dft_OrgPerson',
    'dft_MeetingSeries',
    'dft_Meeting',
    'dft_Attendee',
    'dft_AgendaItem',
    'dft_Paper',
    'dft_Minutes',
    'dft_Transcript',
    'dft_Decision',
    'dft_ActionItem',
    'dft_IntegrationLog',
    'GovernanceBrowseScreen',
    'GovernanceEditScreen',
    'MeetingListScreen',
    'MeetingWorkspaceScreen',
    'AgendaEditorScreen',
    'PaperRegisterScreen',
    'TranscriptImportScreen',
    'MinutesReviewScreen',
    'DecisionsActionsScreen',
    'AttendanceScreen',
    'SettingsScreen',
    'create_governance_body',
    'create_meeting_from_series',
    'build_agenda',
    'import_transcript_and_draft_minutes',
    'chair_review_and_publish',
    'sync_actions_to_planner'
)

Assert-TextContains -RelativePath 'powerplatform/src/app/screens.md' -Patterns @(
    'body -> meeting -> agenda -> votes -> minutes -> decisions/actions chain',
    'MeetingWorkspaceScreen',
    'TranscriptImportScreen',
    'MinutesReviewScreen',
    'DecisionsActionsScreen'
)

Assert-TextContains -RelativePath 'powerplatform/src/flow-contracts.md' -Patterns @(
    'Planner Task Sync',
    'Teams Summary Post',
    'Outlook Invite',
    'Office 365 Users Hierarchy Sync',
    'Transcript To Draft Minutes'
)

Assert-TextContains -RelativePath 'powerplatform/src/schema/dataverse-tables.yaml' -Patterns @(
    'dft_GovernanceBody',
    'dft_TermsOfReference',
    'dft_CommitteeMembership',
    'dft_OrgPerson',
    'dft_MeetingSeries',
    'dft_Meeting',
    'dft_Attendee',
    'dft_AgendaItem',
    'dft_Paper',
    'dft_Minutes',
    'dft_Transcript',
    'dft_Decision',
    'dft_ActionItem',
    'dft_IntegrationLog'
)

Assert-TextContains -RelativePath 'powerplatform/src/flows/flow-catalog.yaml' -Patterns @(
    'planner-sync',
    'outlook-invite-sync',
    'teams-summary-post',
    'org-hierarchy-sync',
    'ai-transcript-minutes',
    'Planner',
    'Microsoft Teams',
    'Office 365 Users',
    'Office 365 Outlook',
    'AI Builder'
)

Assert-TextContains -RelativePath 'tests/smoke/smoke-scenarios.md' -Patterns @(
    'Governance body setup',
    'Meeting creation',
    'Agenda build',
    'Papers',
    'Minutes capture',
    'Decisions',
    'Actions',
    'Planner sync'
)

Assert-TextContains -RelativePath 'tests/smoke/runbook.md' -Patterns @(
    'without writing to Power Platform',
    'seeded governance body, committee, terms of reference, members, meeting series, meeting, agenda items, decisions, and actions',
    'Record outcomes for each scenario'
)

Assert-TextContains -RelativePath 'docs/testing/implementation-readiness.md' -Patterns @(
    'Local readiness checks only cover source artifacts',
    'connector placeholders are deployment blockers',
    'not build blockers'
)

$seed = Get-JsonContent -RelativePath 'powerplatform/src/seed/sample-governance-data.json'

foreach ($key in @('governanceBodies', 'committees', 'termsOfReference', 'members', 'meetingSeries', 'meetings', 'agendaItems', 'decisions', 'actions')) {
    if (-not ($seed.PSObject.Properties.Name -contains $key)) {
        throw "Seed data is missing required top-level key: $key"
    }
}

if (@($seed.governanceBodies).Count -lt 1) { throw 'Seed data must contain at least one governance body.' }
if (@($seed.committees).Count -lt 1) { throw 'Seed data must contain at least one committee.' }
if (@($seed.termsOfReference).Count -lt 1) { throw 'Seed data must contain at least one terms-of-reference record.' }
if (@($seed.members).Count -lt 3) { throw 'Seed data must contain at least three members.' }
if (@($seed.meetingSeries).Count -lt 1) { throw 'Seed data must contain at least one meeting series.' }
if (@($seed.meetings).Count -lt 1) { throw 'Seed data must contain at least one meeting.' }
if (@($seed.agendaItems).Count -lt 2) { throw 'Seed data must contain at least two agenda items.' }
if (@($seed.decisions).Count -lt 1) { throw 'Seed data must contain at least one decision.' }
if (@($seed.actions).Count -lt 1) { throw 'Seed data must contain at least one action.' }

$body = @($seed.governanceBodies)[0]
$committee = @($seed.committees)[0]
$series = @($seed.meetingSeries)[0]
$meeting = @($seed.meetings)[0]
$agendaItems = @($seed.agendaItems)
$decision = @($seed.decisions)[0]
$action = @($seed.actions)[0]

if ($committee.governanceBodyKey -ne $body.key) {
    throw 'Seed committee is not linked to the seeded governance body.'
}

if ($series.committeeKey -ne $committee.key) {
    throw 'Seed meeting series is not linked to the seeded committee.'
}

if ($meeting.seriesKey -ne $series.key) {
    throw 'Seed meeting is not linked to the seeded meeting series.'
}

if (($agendaItems | Where-Object { $_.meetingKey -eq $meeting.key }).Count -lt 2) {
    throw 'Seed agenda items are not linked to the seeded meeting.'
}

if ($decision.meetingKey -ne $meeting.key) {
    throw 'Seed decision is not linked to the seeded meeting.'
}

if ($decision.agendaItemKey -notin @($agendaItems | ForEach-Object { $_.key })) {
    throw 'Seed decision is not linked to a seeded agenda item.'
}

if ($action.meetingKey -ne $meeting.key -or $action.decisionKey -ne $decision.key) {
    throw 'Seed action is not linked to the seeded meeting and decision.'
}

Write-Host "Planning artifact validation completed."

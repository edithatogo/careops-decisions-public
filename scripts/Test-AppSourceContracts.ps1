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
    Write-Verbose "FAIL: $Message"
}

function Add-Warning {
    param([string]$Message)
    $warnings.Add($Message)
    Write-Verbose "WARN: $Message"
}

function Get-ArtifactText {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $path = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        Add-Failure "Missing app source contract artifact: $RelativePath"
        return ''
    }

    $content = Get-Content -LiteralPath $path -Raw
    if ([string]::IsNullOrWhiteSpace($content)) {
        Add-Failure "Empty app source contract artifact: $RelativePath"
        return ''
    }

    return $content
}

function Assert-ContainsText {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string[]]$Patterns
    )
    foreach ($pattern in $Patterns) {
        if ($Content -notmatch [regex]::Escape($pattern)) {
            Add-Failure "$RelativePath is missing required contract text: $pattern"
        }
    }
}

function Assert-ContainsRegex {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$Pattern,
        [Parameter(Mandatory = $true)][string]$Description
    )
    if ($Content -notmatch $Pattern) {
        Add-Failure "$RelativePath is missing required contract: $Description"
    }
}

function Assert-MarkdownHeadings {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string[]]$RequiredHeadings
    )
    foreach ($heading in $RequiredHeadings) {
        $pattern = "(?m)^#+\s+$([regex]::Escape($heading))\s*$"
        if ($Content -notmatch $pattern) {
            Add-Failure "$RelativePath is missing required heading: $heading"
        }
    }
}

function Assert-YamlStructure {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$TopKey
    )
    $match = $Content -match "(?m)^$([regex]::Escape($TopKey)):"
    if (-not $match) {
        Add-Failure "$RelativePath is missing top-level YAML key: $TopKey"
    }
}

$appMapPath = 'powerplatform/src/app/app-map.yaml'
$commandsPath = 'powerplatform/src/app/commands.yaml'
$componentsPath = 'powerplatform/src/app/components.yaml'
$screensPath = 'powerplatform/src/app/screens.md'
$readinessPath = 'docs/testing/implementation-readiness.md'

$appMap = Get-ArtifactText -RelativePath $appMapPath
$commands = Get-ArtifactText -RelativePath $commandsPath
$components = Get-ArtifactText -RelativePath $componentsPath
$screens = Get-ArtifactText -RelativePath $screensPath
$readiness = Get-ArtifactText -RelativePath $readinessPath

$artifactsExist = ($failures.Count -eq 0)

if ($DryRun -and $artifactsExist) {
    Assert-YamlStructure -RelativePath $appMapPath -Content $appMap -TopKey 'app'
    Assert-YamlStructure -RelativePath $commandsPath -Content $commands -TopKey 'commands'
    Assert-YamlStructure -RelativePath $componentsPath -Content $components -TopKey 'components'
}

$coreTables = @(
    'dft_GovernanceBody',
    'dft_TermsOfReference',
    'dft_CommitteeMembership',
    'dft_OrgPerson',
    'dft_OrgRelationship',
    'dft_MeetingSeries',
    'dft_Meeting',
    'dft_Attendee',
    'dft_AgendaItem',
    'dft_Paper',
    'dft_Minutes',
    'dft_Transcript',
    'dft_Decision',
    'dft_ActionItem',
    'dft_Vote',
    'dft_VoteResponse',
    'dft_ConflictOfInterest',
    'dft_AccessRule',
    'dft_AIBuilderRun',
    'dft_IntegrationLog'
)

$coreScreens = @(
    'HomeScreen',
    'GovernanceBrowseScreen',
    'GovernanceEditScreen',
    'OrgHierarchyScreen',
    'MeetingListScreen',
    'MeetingWorkspaceScreen',
    'AgendaEditorScreen',
    'PaperRegisterScreen',
    'VoteScreen',
    'AttendanceScreen',
    'TranscriptImportScreen',
    'MinutesReviewScreen',
    'DecisionsActionsScreen',
    'AccessRulesScreen',
    'AIRunsScreen',
    'IntegrationLogsScreen',
    'SettingsScreen'
)

$coreCommands = @(
    'new_meeting',
    'new_agenda_item',
    'record_vote',
    'import_transcript',
    'draft_minutes',
    'refresh_people',
    'confirm_quorum',
    'publish_minutes',
    'sync_actions_to_planner',
    'review_access_rules',
    'inspect_ai_runs',
    'review_integration_logs'
)

$domainTerms = @(
    'governance',
    'terms of reference',
    'membership',
    'org hierarchy',
    'meeting',
    'agenda',
    'paper',
    'minutes',
    'decision',
    'action',
    'vote',
    'conflict',
    'access rule',
    'AI run',
    'integration log'
)

$componentPatterns = @(
    'deferred_binding_banner',
    'governance_gallery',
    'meeting_gallery',
    'agenda_gallery',
    'paper_gallery',
    'attendee_gallery',
    'vote_gallery',
    'vote_response_gallery',
    'conflict_gallery',
    'access_rule_gallery',
    'org_relationship_gallery',
    'decision_gallery',
    'action_gallery',
    'ai_run_gallery',
    'integration_log_gallery'
)

$formPatterns = @(
    'frmGovernanceBody',
    'frmMeeting',
    'frmAgendaItem',
    'frmPaper',
    'frmMinutes',
    'frmDecision',
    'frmAction',
    'frmVote',
    'frmVoteResponse',
    'frmConflict',
    'frmAccessRule',
    'frmOrgRelationship',
    'frmAIRun',
    'frmIntegrationLog'
)

$deferredBindingPatterns = @(
    'Teams tab deferred',
    'SharePoint document store deferred',
    'Planner binding deferred'
)

if ($artifactsExist) {

    Assert-ContainsText -RelativePath $appMapPath -Content $appMap -Patterns $coreTables
    Assert-ContainsText -RelativePath $appMapPath -Content $appMap -Patterns $coreScreens

    Assert-ContainsText -RelativePath $appMapPath -Content $appMap -Patterns @(
        'route: dashboard',
        'today summary',
        'pending reviews',
        'overdue follow-ups',
        'Dataverse-first app core',
        'deferred Teams, SharePoint, and Planner bindings',
        'home_experience:',
        'heading_behavior:',
        'mode: collapsible',
        'help_surface:',
        'feature_switchboard',
        'check_integrations',
        'deployment_rule: Do not require live Teams, SharePoint, or Planner connectivity to use the core app screens or commands.',
        'planner_sync_enabled: false',
        'teams_post_enabled: false',
        'sharepoint_document_store_enabled: false',
        'create_governance_body',
        'create_meeting_from_series',
        'build_agenda',
        'record_vote',
        'import_transcript_and_draft_minutes',
        'chair_review_and_publish',
        'sync_actions_to_planner',
        'review_access_rules',
        'inspect_ai_runs',
        'inspect_integration_logs'
    )

    Assert-ContainsRegex -RelativePath $appMapPath -Content $appMap -Pattern "(?m)^  screen_map:\r?\n    HomeScreen:" -Description "binding_contracts.screen_map nests HomeScreen"
    Assert-ContainsRegex -RelativePath $appMapPath -Content $appMap -Pattern "(?m)^    MeetingWorkspaceScreen:" -Description "binding_contracts.screen_map nests MeetingWorkspaceScreen"
    if ($appMap -match "(?m)^HomeScreen:") {
        Add-Failure "$appMapPath has HomeScreen at the top level instead of under binding_contracts.screen_map"
    }
    if ($appMap -match "(?m)^  MeetingWorkspaceScreen:") {
        Add-Failure "$appMapPath has MeetingWorkspaceScreen at the binding_contracts level instead of under binding_contracts.screen_map"
    }

    if ($DryRun) {
        $missingScreensFromMap = $coreScreens | Where-Object {
            $appMap -notmatch [regex]::Escape($_)
        }
        foreach ($s in $missingScreensFromMap) {
            Add-Warning "$appMapPath does not reference screen: $s"
        }
    }

    Assert-ContainsText -RelativePath $commandsPath -Content $commands -Patterns $coreCommands
    Assert-ContainsText -RelativePath $commandsPath -Content $commands -Patterns @(
        'runtime: Dataverse only for the core app',
        'live_dependencies:',
        '- none',
        'teams_tab:',
        'sharepoint_document_store:',
        'planner_task_binding:',
        'status: deferred',
        'contract: dataverse_first',
        'dft_IntegrationLog',
        'dft_AccessRule',
        'dft_OrgRelationship',
        'dft_AIBuilderRun',
        'dft_Vote',
        'dft_VoteResponse'
    )

    Assert-ContainsText -RelativePath $componentsPath -Content $components -Patterns $componentPatterns
    Assert-ContainsText -RelativePath $componentsPath -Content $components -Patterns $formPatterns
    Assert-ContainsText -RelativePath $componentsPath -Content $components -Patterns $deferredBindingPatterns

    if ($DryRun) {
        $missingComponentPatterns = $componentPatterns | Where-Object {
            $components -notmatch [regex]::Escape($_)
        }
        foreach ($p in $missingComponentPatterns) {
            Add-Warning "$componentsPath is missing component pattern: $p"
        }

        $missingFormPatterns = $formPatterns | Where-Object {
            $components -notmatch [regex]::Escape($_)
        }
        foreach ($p in $missingFormPatterns) {
            Add-Warning "$componentsPath is missing form pattern: $p"
        }
    }

    Assert-ContainsText -RelativePath $screensPath -Content $screens -Patterns $coreScreens
    Assert-ContainsText -RelativePath $screensPath -Content $screens -Patterns $domainTerms
    Assert-ContainsText -RelativePath $screensPath -Content $screens -Patterns @(
        'Org hierarchy and reporting-line panel.',
        'Dataverse-first core',
        'Teams, SharePoint, and Planner are deferred bindings rather than runtime requirements for the core screens.',
        'Workflow launch cards',
        'galHomeLaunchCards',
        'Setup health',
        'Deferred binding status for Teams, SharePoint, and Planner.',
        'Deferred SharePoint document store placeholder.',
        'Deferred Planner binding state.',
        'Deferred binding setup.',
        'body -> meeting -> agenda -> votes -> minutes -> decisions/actions chain',
        'The home navigation headings are treated as collapsible groups in the shell contract.'
    )

    if ($DryRun) {
        $screensHeadings = @(
            'HomeScreen',
            'GovernanceBrowseScreen',
            'GovernanceEditScreen',
            'OrgHierarchyScreen',
            'MeetingListScreen',
            'MeetingWorkspaceScreen',
            'AgendaEditorScreen',
            'PaperRegisterScreen',
            'VoteScreen',
            'AttendanceScreen',
            'TranscriptImportScreen',
            'MinutesReviewScreen',
            'DecisionsActionsScreen',
            'AccessRulesScreen',
            'AIRunsScreen',
            'IntegrationLogsScreen',
            'SettingsScreen'
        )

        $missingScreenHeadings = $screensHeadings | Where-Object {
            $screens -notmatch "(?m)^###?\s+$([regex]::Escape($_))\s*$"
        }
        foreach ($s in $missingScreenHeadings) {
            Add-Warning "$screensPath is missing a dedicated heading for screen: $s"
        }
    }

    Assert-ContainsText -RelativePath $readinessPath -Content $readiness -Patterns @(
        'connector placeholders are deployment blockers',
        'not build blockers'
    )

    foreach ($commandName in $coreCommands) {
        Assert-ContainsRegex -RelativePath $commandsPath -Content $commands -Pattern "(?ms)^\s{2}$([regex]::Escape($commandName)):\s+.*?^\s{4}binds_to:\s+.*?^\s{4}contract:\s+dataverse_first" -Description "command $commandName has Dataverse bindings and contract: dataverse_first"
    }

    if ($DryRun) {
        foreach ($table in $coreTables) {
            $tableSuffix = $table -replace '^dft_', ''
            $tableLower = $table.ToLowerInvariant()
            $foundInAppMap = $appMap -match [regex]::Escape($table)
            $foundInCommands = $commands -match [regex]::Escape($table)
            $foundInComponents = $components -match [regex]::Escape($table)
            if (-not $foundInAppMap -and -not $foundInCommands -and -not $foundInComponents) {
                Add-Warning "Table $table is not referenced in any app contract artifact."
            }
        }
    }
}

if ($PassThru) {
    [pscustomobject]@{
        Passed    = ($failures.Count -eq 0)
        Failures  = @($failures)
        Warnings  = @($warnings)
        Summary   = "App source contract validation with $($failures.Count) failures and $($warnings.Count) warnings."
    }
    return
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

if ($warnings.Count -gt 0) {
    $warnings | ForEach-Object { Write-Warning $_ }
}

Write-Host "App source contract validation passed. ($($failures.Count) failures, $($warnings.Count) warnings)"

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$SeedPath = "powerplatform/src/seed/medication-safety-sample-data.json",
    [string]$EnvironmentUrl = "https://environment.example.invalid/",
    [switch]$WhatIfOnly
)

$ErrorActionPreference = 'Stop'

function Resolve-RepoPath {
    param([Parameter(Mandatory = $true)][string]$PathValue)

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return $PathValue
    }

    return Join-Path $repoRoot $PathValue
}

function ConvertTo-ODataStringLiteral {
    param([Parameter(Mandatory = $true)][string]$Value)
    return "'" + ($Value -replace "'", "''") + "'"
}

function Invoke-Dataverse {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Path,
        [hashtable]$Body
    )

    $uri = "$EnvironmentUrl/api/data/v9.2/$Path"
    $parameters = @{
        Method = $Method
        Uri = $uri
        Headers = $script:headers
    }

    if ($Body) {
        $parameters.Body = ($Body | ConvertTo-Json -Depth 20)
        $parameters.ContentType = 'application/json'
    }

    Invoke-RestMethod @parameters
}

function Get-ExistingId {
    param(
        [Parameter(Mandatory = $true)][string]$EntitySet,
        [Parameter(Mandatory = $true)][string]$IdColumn,
        [Parameter(Mandatory = $true)][string]$KeyColumn,
        [Parameter(Mandatory = $true)][string]$KeyValue
    )

    $filter = "$KeyColumn eq $(ConvertTo-ODataStringLiteral $KeyValue)"
    $path = "$EntitySet?`$select=$IdColumn&`$filter=$([uri]::EscapeDataString($filter))"
    $result = Invoke-Dataverse -Method Get -Path $path
    $rows = @($result.value)
    if ($rows.Count -gt 0) {
        return [string]$rows[0].$IdColumn
    }

    return $null
}

function Set-LookupBinding {
    param(
        [Parameter(Mandatory = $true)][hashtable]$Body,
        [Parameter(Mandatory = $true)][string]$LookupColumn,
        [Parameter(Mandatory = $true)][string]$EntitySet,
        [string]$Id
    )

    if ([string]::IsNullOrWhiteSpace($Id)) {
        return
    }

    $Body["$LookupColumn@odata.bind"] = "/$EntitySet($Id)"
}

function Upsert-Record {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][string]$EntitySet,
        [Parameter(Mandatory = $true)][string]$IdColumn,
        [Parameter(Mandatory = $true)][string]$KeyColumn,
        [Parameter(Mandatory = $true)][string]$KeyValue,
        [Parameter(Mandatory = $true)][hashtable]$Body
    )

    $existingId = Get-ExistingId -EntitySet $EntitySet -IdColumn $IdColumn -KeyColumn $KeyColumn -KeyValue $KeyValue
    if ($WhatIfOnly) {
        $mode = if ($existingId) { 'update' } else { 'create' }
        Write-Host "WHATIF $mode $Label [$EntitySet] $KeyColumn=$KeyValue"
        return $existingId
    }

    if ($existingId) {
        if ($PSCmdlet.ShouldProcess("$EntitySet/$existingId", "Update $Label")) {
            Invoke-Dataverse -Method Patch -Path "$EntitySet($existingId)" -Body $Body | Out-Null
        }
        Write-Host "Updated $Label"
        return $existingId
    }

    if ($PSCmdlet.ShouldProcess($EntitySet, "Create $Label")) {
        $response = Invoke-Dataverse -Method Post -Path $EntitySet -Body $Body
        $newId = [string]$response.$IdColumn
        if ([string]::IsNullOrWhiteSpace($newId)) {
            $newId = Get-ExistingId -EntitySet $EntitySet -IdColumn $IdColumn -KeyColumn $KeyColumn -KeyValue $KeyValue
        }
        Write-Host "Created $Label"
        return $newId
    }
}

function Get-SeedPersonId {
    param([string]$PersonKey)
    if ([string]::IsNullOrWhiteSpace($PersonKey) -or -not $personIds.ContainsKey($PersonKey)) {
        return $null
    }
    return $personIds[$PersonKey]
}

function Get-MeetingId {
    param([string]$MeetingKey)
    if ([string]::IsNullOrWhiteSpace($MeetingKey) -or -not $meetingIds.ContainsKey($MeetingKey)) {
        return $null
    }
    return $meetingIds[$MeetingKey]
}

function Get-AgendaItemId {
    param([string]$AgendaItemKey)
    if ([string]::IsNullOrWhiteSpace($AgendaItemKey) -or -not $agendaItemIds.ContainsKey($AgendaItemKey)) {
        return $null
    }
    return $agendaItemIds[$AgendaItemKey]
}

function Get-DecisionId {
    param([string]$DecisionKey)
    if ([string]::IsNullOrWhiteSpace($DecisionKey) -or -not $decisionIds.ContainsKey($DecisionKey)) {
        return $null
    }
    return $decisionIds[$DecisionKey]
}

function Get-TranscriptId {
    param([string]$TranscriptKey)
    if ([string]::IsNullOrWhiteSpace($TranscriptKey) -or -not $transcriptIds.ContainsKey($TranscriptKey)) {
        return $null
    }
    return $transcriptIds[$TranscriptKey]
}

function Assert-MedicationSafetySeedContract {
    param(
        [Parameter(Mandatory = $true)]
        $Seed
    )

    $requiredCollections = @(
        'governanceBodies',
        'committees',
        'termsOfReference',
        'members',
        'orgRelationships',
        'featureDefinitions',
        'meetingSeriesFeatureDefaults',
        'meetingFeatureOverrides',
        'meetingSeries',
        'meetings',
        'agendaItems',
        'papers',
        'minutes',
        'decisions',
        'actions',
        'accessRules',
        'integrationBindings',
        'integrationLogs',
        'aiBuilderPrompts',
        'aiBuilderRuns'
    )

    foreach ($collection in $requiredCollections) {
        if (-not ($Seed.PSObject.Properties.Name -contains $collection)) {
            throw "Medication Safety seed is missing required top-level collection: $collection"
        }
        if (@($Seed.$collection).Count -lt 1) {
            throw "Medication Safety seed must contain at least one record in '$collection'."
        }
    }

    if ([string]::IsNullOrWhiteSpace([string]$Seed.sourceNotice) -or [string]$Seed.sourceNotice -notmatch 'Synthetic') {
        throw 'Medication Safety seed must explicitly declare itself as synthetic.'
    }

    $members = @($Seed.members)
    if ((@($members | Where-Object { $_.role -eq 'chair' }).Count) -lt 1) {
        throw 'Medication Safety seed must include a chair member.'
    }
    if ((@($members | Where-Object { $_.role -eq 'secretary' }).Count) -lt 1) {
        throw 'Medication Safety seed must include a secretariat member.'
    }

    foreach ($member in $members) {
        if ([string]::IsNullOrWhiteSpace($member.mail) -or $member.mail -notmatch '@example\.invalid$') {
            throw "Medication Safety seed member '$($member.key)' must use an example.invalid mailbox."
        }
    }

    if ((@($Seed.minutes | Where-Object { $_.status -eq 'approved' }).Count) -lt 1) {
        throw 'Medication Safety seed must include at least one approved minutes record.'
    }

    if ((@($Seed.attendees | Where-Object { $_.attendeeRole -eq 'chair' }).Count) -lt 1) {
        throw 'Medication Safety seed must include a chair attendee row.'
    }

    if ((@($Seed.integrationLogs | Where-Object { $_.status -eq 'succeeded' }).Count) -lt 1) {
        throw 'Medication Safety seed must include a succeeded integration log row for import validation evidence.'
    }
    if ((@($Seed.integrationLogs | Where-Object { $_.status -eq 'skipped' }).Count) -lt 1) {
        throw 'Medication Safety seed must include a skipped integration log row for the real-data source gate.'
    }

    if ((@($Seed.aiBuilderPrompts | Where-Object { $_.approvalStatus -eq 'approved' }).Count) -lt 1) {
        throw 'Medication Safety seed must include at least one approved AI Builder prompt.'
    }

    if ((@($Seed.aiBuilderRuns | Where-Object { $_.runStatus -eq 'skipped' }).Count) -lt 1) {
        throw 'Medication Safety seed must include a skipped AI Builder run record.'
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$seedFullPath = Resolve-RepoPath $SeedPath

if (-not (Test-Path -LiteralPath $seedFullPath)) {
    throw "Seed file not found: $seedFullPath"
}

$seed = Get-Content -LiteralPath $seedFullPath -Raw | ConvertFrom-Json
Assert-MedicationSafetySeedContract -Seed $seed

if ($WhatIfOnly) {
    $summary = [ordered]@{
        seedPath = $seedFullPath
        sourceNotice = $seed.sourceNotice
        counts = [ordered]@{
            governanceBodies = @($seed.governanceBodies).Count
            committees = @($seed.committees).Count
            termsOfReference = @($seed.termsOfReference).Count
            members = @($seed.members).Count
            meetingSeriesFeatureDefaults = @($seed.meetingSeriesFeatureDefaults).Count
            meetingFeatureOverrides = @($seed.meetingFeatureOverrides).Count
            meetingSeries = @($seed.meetingSeries).Count
            meetings = @($seed.meetings).Count
            agendaItems = @($seed.agendaItems).Count
            papers = @($seed.papers).Count
            minutes = @($seed.minutes).Count
            decisions = @($seed.decisions).Count
            actions = @($seed.actions).Count
            accessRules = @($seed.accessRules).Count
            integrationBindings = @($seed.integrationBindings).Count
            integrationLogs = @($seed.integrationLogs).Count
            aiBuilderPrompts = @($seed.aiBuilderPrompts).Count
            aiBuilderRuns = @($seed.aiBuilderRuns).Count
        }
        validation = 'passed'
        note = 'Offline validation only. Real NSW data remains gated to Outlook, SharePoint, Teams, or user-provided exports.'
    }

    Write-Host 'Medication Safety sample validation passed.'
    foreach ($key in $summary.counts.Keys) {
        Write-Host ("- {0}: {1}" -f $key, $summary.counts[$key])
    }
    return [pscustomobject]$summary
}

$tokenResponse = az account get-access-token --resource $EnvironmentUrl --output json | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace([string]$tokenResponse.accessToken)) {
    throw "Azure CLI did not return a Dataverse access token for $EnvironmentUrl."
}

$script:headers = @{
    Authorization = "Bearer $($tokenResponse.accessToken)"
    Accept = 'application/json'
    'OData-MaxVersion' = '4.0'
    'OData-Version' = '4.0'
    Prefer = 'return=representation'
}

$personIds = @{}
$governanceBodyIds = @{}
$seriesIds = @{}
$meetingIds = @{}
$agendaItemIds = @{}
$paperIds = @{}
$decisionIds = @{}
$integrationLogIds = @{}
$promptIds = @{}
$transcriptIds = @{}
$aiBuilderRunIds = @{}

foreach ($member in @($seed.members)) {
    $body = @{
        dft_name = $member.displayName
        dft_aadobjectid = $member.key
        dft_workemail = $member.mail
        dft_userprincipalname = $member.mail
        dft_jobtitle = $member.jobTitle
        dft_department = $member.department
    }
    if ($member.managerMail) { $body.dft_manageremail = $member.managerMail }
    if ($member.lastSyncedOn) { $body.dft_lastsyncedon = $member.lastSyncedOn }

    $personIds[$member.key] = Upsert-Record -Label "person $($member.displayName)" -EntitySet 'dft_orgpersons' -IdColumn 'dft_orgpersonid' -KeyColumn 'dft_workemail' -KeyValue $member.mail -Body $body
}

foreach ($bodySeed in @($seed.governanceBodies)) {
    $body = @{
        dft_name = $bodySeed.name
        dft_bodycode = $bodySeed.key
        dft_description = $bodySeed.purpose
    }
    Set-LookupBinding -Body $body -LookupColumn 'dft_ownerpersonid' -EntitySet 'dft_orgpersons' -Id (Get-SeedPersonId $bodySeed.ownerPersonKey)

    $governanceBodyIds[$bodySeed.key] = Upsert-Record -Label "governance body $($bodySeed.name)" -EntitySet 'dft_governancebodies' -IdColumn 'dft_governancebodyid' -KeyColumn 'dft_bodycode' -KeyValue $bodySeed.key -Body $body
}

foreach ($committee in @($seed.committees)) {
    $body = @{
        dft_name = $committee.name
        dft_bodycode = $committee.key
        dft_description = "Committee under $($committee.governanceBodyKey)."
    }
    Set-LookupBinding -Body $body -LookupColumn 'dft_parentgovernancebodyid' -EntitySet 'dft_governancebodies' -Id $governanceBodyIds[$committee.governanceBodyKey]
    Set-LookupBinding -Body $body -LookupColumn 'dft_ownerpersonid' -EntitySet 'dft_orgpersons' -Id (Get-SeedPersonId $committee.ownerPersonKey)

    $governanceBodyIds[$committee.key] = Upsert-Record -Label "committee $($committee.name)" -EntitySet 'dft_governancebodies' -IdColumn 'dft_governancebodyid' -KeyColumn 'dft_bodycode' -KeyValue $committee.key -Body $body
}

foreach ($tor in @($seed.termsOfReference)) {
    $body = @{
        dft_name = $tor.title
        dft_versionnumber = [decimal]$tor.version
        dft_effectivefrom = $tor.effectiveFrom
        dft_reviewdueon = $tor.reviewDate
        dft_quorumnumber = [int]$tor.quorumNumber
    }
    Set-LookupBinding -Body $body -LookupColumn 'dft_governancebodyid' -EntitySet 'dft_governancebodies' -Id $governanceBodyIds[$tor.committeeKey]
    Upsert-Record -Label "terms of reference $($tor.title)" -EntitySet 'dft_termsofreferences' -IdColumn 'dft_termsofreferenceid' -KeyColumn 'dft_name' -KeyValue $tor.title -Body $body | Out-Null
}

foreach ($member in @($seed.members)) {
    $body = @{
        dft_name = "$($member.displayName) - $($member.role)"
        dft_votingrights = [bool]$member.votingRights
        dft_isalternate = [bool]$member.isAlternate
        dft_startdate = $member.startDate
    }
    Set-LookupBinding -Body $body -LookupColumn 'dft_governancebodyid' -EntitySet 'dft_governancebodies' -Id $governanceBodyIds[$member.committeeKey]
    Set-LookupBinding -Body $body -LookupColumn 'dft_orgpersonid' -EntitySet 'dft_orgpersons' -Id (Get-SeedPersonId $member.key)
    Upsert-Record -Label "membership $($member.displayName)" -EntitySet 'dft_committeememberships' -IdColumn 'dft_committeemembershipid' -KeyColumn 'dft_name' -KeyValue "$($member.displayName) - $($member.role)" -Body $body | Out-Null
}

foreach ($promptSeed in @($seed.aiBuilderPrompts)) {
    $body = @{
        dft_name = $promptSeed.purpose
        dft_promptname = $promptSeed.promptName
        dft_version = [decimal]$promptSeed.version
        dft_classificationallowance = $promptSeed.classificationAllowance
        dft_approvalstatus = $promptSeed.approvalStatus
        dft_effectivefrom = $promptSeed.effectiveFrom
        dft_description = $promptSeed.description
    }
    if ($promptSeed.effectiveUntil) { $body.dft_effectiveuntil = $promptSeed.effectiveUntil }
    if ($promptSeed.approvedOn) { $body.dft_approvedon = $promptSeed.approvedOn }
    Set-LookupBinding -Body $body -LookupColumn 'dft_ownerid' -EntitySet 'dft_orgpersons' -Id (Get-SeedPersonId $promptSeed.ownerPersonKey)
    if ($promptSeed.approvedByPersonKey) {
        Set-LookupBinding -Body $body -LookupColumn 'dft_approvedbyid' -EntitySet 'dft_orgpersons' -Id (Get-SeedPersonId $promptSeed.approvedByPersonKey)
    }

    $promptIds[$promptSeed.key] = Upsert-Record -Label "AI Builder prompt $($promptSeed.promptName)" -EntitySet 'dft_aibuilderprompts' -IdColumn 'dft_aibuilderpromptid' -KeyColumn 'dft_promptname' -KeyValue $promptSeed.promptName -Body $body
}

foreach ($series in @($seed.meetingSeries)) {
    $body = @{
        dft_name = $series.name
        dft_seriescode = $series.key
        dft_recurrencerule = $series.recurrenceRule
        dft_defaultlocation = $series.defaultLocation
        dft_defaultchannelid = $series.defaultChannelId
    }
    Set-LookupBinding -Body $body -LookupColumn 'dft_governancebodyid' -EntitySet 'dft_governancebodies' -Id $governanceBodyIds[$series.committeeKey]
    Set-LookupBinding -Body $body -LookupColumn 'dft_seriesownerid' -EntitySet 'dft_orgpersons' -Id (Get-SeedPersonId $series.seriesOwnerPersonKey)

    $seriesIds[$series.key] = Upsert-Record -Label "meeting series $($series.name)" -EntitySet 'dft_meetingserieses' -IdColumn 'dft_meetingseriesid' -KeyColumn 'dft_seriescode' -KeyValue $series.key -Body $body
}

foreach ($meeting in @($seed.meetings)) {
    $body = @{
        dft_name = $meeting.title
        dft_meetingnumber = [int]$meeting.meetingNumber
        dft_scheduledstartutc = $meeting.scheduledStartUtc
        dft_scheduledendutc = $meeting.scheduledEndUtc
        dft_externalcalendarid = $meeting.externalCalendarId
        dft_externalteamsid = $meeting.externalTeamsId
        dft_location = $meeting.location
    }
    Set-LookupBinding -Body $body -LookupColumn 'dft_meetingseriesid' -EntitySet 'dft_meetingserieses' -Id $seriesIds[$meeting.seriesKey]
    Set-LookupBinding -Body $body -LookupColumn 'dft_chairpersonid' -EntitySet 'dft_orgpersons' -Id (Get-SeedPersonId $meeting.chairPersonKey)

    $meetingIds[$meeting.key] = Upsert-Record -Label "meeting $($meeting.title)" -EntitySet 'dft_meetings' -IdColumn 'dft_meetingid' -KeyColumn 'dft_externalcalendarid' -KeyValue $meeting.externalCalendarId -Body $body
}

foreach ($item in @($seed.agendaItems)) {
    $body = @{
        dft_name = "$($item.meetingKey):$($item.sequence)"
        dft_agendatitle = $item.title
        dft_sequence = [int]$item.sequence
        dft_timeminutes = [int]$item.durationMinutes
        dft_decisionsought = [bool]$item.decisionSought
    }
    Set-LookupBinding -Body $body -LookupColumn 'dft_meetingid' -EntitySet 'dft_meetings' -Id (Get-MeetingId $item.meetingKey)
    Set-LookupBinding -Body $body -LookupColumn 'dft_presenterid' -EntitySet 'dft_orgpersons' -Id (Get-SeedPersonId $item.presenterPersonKey)

    $agendaItemIds[$item.key] = Upsert-Record -Label "agenda item $($item.title)" -EntitySet 'dft_agendaitems' -IdColumn 'dft_agendaitemid' -KeyColumn 'dft_name' -KeyValue "$($item.meetingKey):$($item.sequence)" -Body $body
}

foreach ($paper in @($seed.papers)) {
    $body = @{
        dft_name = $paper.title
        dft_documentid = $paper.documentId
        dft_sharepointurl = $paper.sharepointUrl
    }
    Set-LookupBinding -Body $body -LookupColumn 'dft_meetingid' -EntitySet 'dft_meetings' -Id (Get-MeetingId $paper.meetingKey)
    Set-LookupBinding -Body $body -LookupColumn 'dft_agendaitemid' -EntitySet 'dft_agendaitems' -Id (Get-AgendaItemId $paper.agendaItemKey)
    Set-LookupBinding -Body $body -LookupColumn 'dft_ownerid' -EntitySet 'dft_orgpersons' -Id (Get-SeedPersonId $paper.ownerPersonKey)

    $paperIds[$paper.key] = Upsert-Record -Label "paper $($paper.title)" -EntitySet 'dft_papers' -IdColumn 'dft_paperid' -KeyColumn 'dft_documentid' -KeyValue $paper.documentId -Body $body
}

foreach ($minutes in @($seed.minutes)) {
    $body = @{
        dft_name = $minutes.title
        dft_status = $minutes.status
        dft_versionnumber = [decimal]$minutes.version
        dft_drafttext = $minutes.draftText
    }
    Set-LookupBinding -Body $body -LookupColumn 'dft_meetingid' -EntitySet 'dft_meetings' -Id (Get-MeetingId $minutes.meetingKey)
    Set-LookupBinding -Body $body -LookupColumn 'dft_approvedbyid' -EntitySet 'dft_orgpersons' -Id (Get-SeedPersonId $minutes.approvedByPersonKey)
    if ($minutes.approvedOn) { $body.dft_approvedon = $minutes.approvedOn }
    if ($minutes.publishedOn) { $body.dft_publishedon = $minutes.publishedOn }
    if ($minutes.publishedUrl) { $body.dft_publishedurl = $minutes.publishedUrl }
    Upsert-Record -Label "minutes $($minutes.title)" -EntitySet 'dft_minuteses' -IdColumn 'dft_minutesid' -KeyColumn 'dft_name' -KeyValue $minutes.title -Body $body | Out-Null
}

foreach ($decision in @($seed.decisions)) {
    $body = @{
        dft_name = $decision.title
        dft_decisionnumber = $decision.decisionNumber
        dft_decisiontext = $decision.decisionText
        dft_authority = $decision.authority
        dft_effectiveon = $decision.effectiveOn
        dft_reviewdueon = $decision.reviewDueOn
    }
    Set-LookupBinding -Body $body -LookupColumn 'dft_meetingid' -EntitySet 'dft_meetings' -Id (Get-MeetingId $decision.meetingKey)
    Set-LookupBinding -Body $body -LookupColumn 'dft_agendaitemid' -EntitySet 'dft_agendaitems' -Id (Get-AgendaItemId $decision.agendaItemKey)
    Set-LookupBinding -Body $body -LookupColumn 'dft_ownerid' -EntitySet 'dft_orgpersons' -Id (Get-SeedPersonId $decision.ownerPersonKey)

    $decisionIds[$decision.key] = Upsert-Record -Label "decision $($decision.title)" -EntitySet 'dft_decisions' -IdColumn 'dft_decisionid' -KeyColumn 'dft_decisionnumber' -KeyValue $decision.decisionNumber -Body $body
}

foreach ($action in @($seed.actions)) {
    $body = @{
        dft_name = $action.title
        dft_actionnumber = $action.actionNumber
        dft_actionsummary = $action.title
        dft_duedate = $action.dueDate
        dft_completionevidence = $action.completionEvidence
    }
    if ($action.plannerTaskId) { $body.dft_plannertaskid = $action.plannerTaskId }
    Set-LookupBinding -Body $body -LookupColumn 'dft_meetingid' -EntitySet 'dft_meetings' -Id (Get-MeetingId $action.meetingKey)
    Set-LookupBinding -Body $body -LookupColumn 'dft_agendaitemid' -EntitySet 'dft_agendaitems' -Id (Get-AgendaItemId $action.agendaItemKey)
    Set-LookupBinding -Body $body -LookupColumn 'dft_decisionid' -EntitySet 'dft_decisions' -Id (Get-DecisionId $action.decisionKey)
    Set-LookupBinding -Body $body -LookupColumn 'dft_ownerid' -EntitySet 'dft_orgpersons' -Id (Get-SeedPersonId $action.ownerPersonKey)
    Upsert-Record -Label "action $($action.actionNumber)" -EntitySet 'dft_actionitems' -IdColumn 'dft_actionitemid' -KeyColumn 'dft_actionnumber' -KeyValue $action.actionNumber -Body $body | Out-Null
}

foreach ($attendee in @($seed.attendees)) {
    $body = @{
        dft_name = "$($attendee.meetingKey):$($attendee.key)"
        dft_displayname = $attendee.displayName
        dft_attendeerole = $attendee.attendeeRole
        dft_attendancestatus = $attendee.attendanceStatus
        dft_response = $attendee.response
        dft_votingeligible = [bool]$attendee.votingEligible
        dft_isproxy = $false
    }
    Set-LookupBinding -Body $body -LookupColumn 'dft_meetingid' -EntitySet 'dft_meetings' -Id (Get-MeetingId $attendee.meetingKey)
    Set-LookupBinding -Body $body -LookupColumn 'dft_orgpersonid' -EntitySet 'dft_orgpersons' -Id (Get-SeedPersonId $attendee.personKey)
    if ($attendee.invitedOn) { $body.dft_invitedon = $attendee.invitedOn }
    if ($attendee.responseUpdatedOn) { $body.dft_responseupdatedon = $attendee.responseUpdatedOn }
    Upsert-Record -Label "attendee $($attendee.displayName)" -EntitySet 'dft_attendees' -IdColumn 'dft_attendeeid' -KeyColumn 'dft_name' -KeyValue "$($attendee.meetingKey):$($attendee.key)" -Body $body | Out-Null
}

foreach ($transcript in @($seed.transcripts)) {
    $body = @{
        dft_name = $transcript.title
        dft_transcripttext = $transcript.transcriptText
        dft_fileurl = "https://example.invalid/nsw/medication-safety/2026-05/transcript-placeholder.txt"
    }
    Set-LookupBinding -Body $body -LookupColumn 'dft_meetingid' -EntitySet 'dft_meetings' -Id (Get-MeetingId $transcript.meetingKey)
    $transcriptIds[$transcript.key] = Upsert-Record -Label "transcript placeholder $($transcript.title)" -EntitySet 'dft_transcripts' -IdColumn 'dft_transcriptid' -KeyColumn 'dft_name' -KeyValue $transcript.title -Body $body
}

foreach ($run in @($seed.aiBuilderRuns)) {
    $body = @{
        dft_name = $run.name
        dft_inputhash = $run.inputHash
        dft_runstatus = $run.runStatus
        dft_humanreviewstatus = $run.humanReviewStatus
        dft_modelprovider = $run.modelProvider
        dft_starttime = $run.startTime
        dft_completiontime = $run.completionTime
        dft_retrycount = [int]$run.retryCount
        dft_exceptionnotes = $run.exceptionNotes
    }
    if ($run.outputHash) { $body.dft_outputhash = $run.outputHash }
    if ($run.errorMessage) { $body.dft_errormessage = $run.errorMessage }
    if ($run.reviewedOn) { $body.dft_reviewedon = $run.reviewedOn }
    Set-LookupBinding -Body $body -LookupColumn 'dft_meetingid' -EntitySet 'dft_meetings' -Id (Get-MeetingId $run.meetingKey)
    Set-LookupBinding -Body $body -LookupColumn 'dft_promptid' -EntitySet 'dft_aibuilderprompts' -Id $promptIds[$run.promptKey]
    Set-LookupBinding -Body $body -LookupColumn 'dft_transcriptid' -EntitySet 'dft_transcripts' -Id (Get-TranscriptId $run.transcriptKey)
    Set-LookupBinding -Body $body -LookupColumn 'dft_requesterid' -EntitySet 'dft_orgpersons' -Id (Get-SeedPersonId $run.requesterPersonKey)
    if ($run.reviewerPersonKey) {
        Set-LookupBinding -Body $body -LookupColumn 'dft_reviewerid' -EntitySet 'dft_orgpersons' -Id (Get-SeedPersonId $run.reviewerPersonKey)
    }

    $aiBuilderRunIds[$run.key] = Upsert-Record -Label "AI Builder run $($run.name)" -EntitySet 'dft_aibuilderruns' -IdColumn 'dft_aibuilderrunid' -KeyColumn 'dft_name' -KeyValue $run.name -Body $body
}

foreach ($log in @($seed.integrationLogs)) {
    $body = @{
        dft_name = $log.operation
        dft_sourcesystem = $log.sourceSystem
        dft_targetsystem = $log.targetSystem
        dft_recordtype = $log.recordType
        dft_operation = $log.operation
        dft_requestsummary = $log.requestSummary
        dft_status = $log.status
        dft_message = $log.message
        dft_relatedrecordid = $log.relatedRecordId
        dft_actor = $log.actor
        dft_correlationid = $log.correlationId
        dft_retrycount = [int]$log.retryCount
        dft_errormessage = $log.errorMessage
        dft_loggedon = $log.loggedOn
    }

    $integrationLogIds[$log.key] = Upsert-Record -Label "integration log $($log.operation)" -EntitySet 'dft_integrationlogs' -IdColumn 'dft_integrationlogid' -KeyColumn 'dft_correlationid' -KeyValue $log.correlationId -Body $body
}

Write-Host "Medication Safety sample import complete for $EnvironmentUrl."

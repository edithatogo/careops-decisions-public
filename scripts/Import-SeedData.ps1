[CmdletBinding()]
param(
    [string]$SeedPath = "powerplatform/src/seed/sample-governance-data.json",
    [string]$OutputPath = "artifacts/seed/sample-governance-import-plan.json",
    [string]$PayloadPath = "artifacts/seed/sample-governance-dataverse-payloads.json",
    [switch]$DryRun,
    [switch]$WritePayloads,
    [switch]$Json
)

$ErrorActionPreference = 'Stop'

function Resolve-RepoPath {
    param([Parameter(Mandatory = $true)][string]$PathValue)

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return $PathValue
    }

    return Join-Path $repoRoot $PathValue
}

function Get-SeedItems {
    param(
        [Parameter(Mandatory = $true)][object]$Seed,
        [Parameter(Mandatory = $true)][string]$Collection
    )

    return @($Seed.$Collection)
}

function Get-SeedPropertyValue {
    param(
        [Parameter(Mandatory = $true)][object]$Item,
        [Parameter(Mandatory = $true)][string]$PropertyName
    )

    $property = $Item.PSObject.Properties[$PropertyName]
    if (-not $property) {
        return $null
    }

    return $property.Value
}

function New-SeedIndex {
    param(
        [Parameter(Mandatory = $true)][object[]]$Items,
        [Parameter(Mandatory = $true)][string]$KeyProperty,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $index = @{}
    foreach ($item in $Items) {
        $key = [string](Get-SeedPropertyValue -Item $item -PropertyName $KeyProperty)
        if ([string]::IsNullOrWhiteSpace($key)) {
            throw "$Label item is missing required property '$KeyProperty'."
        }

        if ($index.ContainsKey($key)) {
            throw "Duplicate $Label key detected: $key"
        }

        $index[$key] = $item
    }

    return $index
}

function Assert-ReferenceExists {
    param(
        [Parameter(Mandatory = $true)][string]$Value,
        [Parameter(Mandatory = $true)][hashtable]$Index,
        [Parameter(Mandatory = $true)][string]$ReferenceName,
        [Parameter(Mandatory = $true)][string]$Context
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "$Context is missing required reference '$ReferenceName'."
    }

    if (-not $Index.ContainsKey($Value)) {
        throw "$Context references unknown $ReferenceName '$Value'."
    }
}

function Assert-OptionalReferenceExists {
    param(
        [string]$Value,
        [Parameter(Mandatory = $true)][hashtable]$Index,
        [Parameter(Mandatory = $true)][string]$ReferenceName,
        [Parameter(Mandatory = $true)][string]$Context
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return
    }

    if (-not $Index.ContainsKey($Value)) {
        throw "$Context references unknown $ReferenceName '$Value'."
    }
}

function Resolve-PersonMail {
    param(
        [string]$PersonKey,
        [Parameter(Mandatory = $true)][hashtable]$PeopleByKey
    )

    if ([string]::IsNullOrWhiteSpace($PersonKey)) {
        return $null
    }

    if ($PeopleByKey.ContainsKey($PersonKey)) {
        return [string](Get-SeedPropertyValue -Item $PeopleByKey[$PersonKey] -PropertyName 'mail')
    }

    return $PersonKey
}

function New-UpsertStep {
    param(
        [Parameter(Mandatory = $true)][string]$Table,
        [Parameter(Mandatory = $true)][string]$SourceCollection,
        [Parameter(Mandatory = $true)][string]$SourceKey,
        [Parameter(Mandatory = $true)][string]$NaturalKey,
        [Parameter(Mandatory = $true)][hashtable]$FieldMap,
        [hashtable]$Lookups = @{}
    )

    [pscustomobject]@{
        table = $Table
        sourceCollection = $SourceCollection
        sourceKey = $SourceKey
        naturalKey = $NaturalKey
        fieldMap = $FieldMap
        lookups = $Lookups
        mode = 'upsert-plan'
    }
}

function New-DataversePayload {
    param(
        [Parameter(Mandatory = $true)][object]$Step,
        [Parameter(Mandatory = $true)][hashtable]$LookupTargets
    )

    $payload = [ordered]@{}
    foreach ($key in @($Step.fieldMap.Keys | Sort-Object)) {
        $value = $Step.fieldMap[$key]
        if ($null -ne $value) {
            $payload[$key] = $value
        }
    }

    $lookupBindings = [ordered]@{}
    foreach ($key in @($Step.lookups.Keys | Sort-Object)) {
        $value = $Step.lookups[$key]
        if ($null -eq $value -or [string]::IsNullOrWhiteSpace([string]$value)) {
            continue
        }

        $target = if ($LookupTargets.ContainsKey($key)) {
            $LookupTargets[$key]
        } else {
            [pscustomobject]@{
                table = $null
                naturalKeyField = $null
            }
        }

        $lookupBindings[$key] = [ordered]@{
            targetTable = $target.table
            targetNaturalKeyField = $target.naturalKeyField
            targetNaturalKey = $value
        }
    }

    [pscustomobject]@{
        table = $Step.table
        naturalKey = $Step.naturalKey
        sourceCollection = $Step.sourceCollection
        sourceKey = $Step.sourceKey
        payload = $payload
        lookupBindings = $lookupBindings
        mode = 'dataverse-upsert-payload'
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$seedFullPath = Resolve-RepoPath $SeedPath
$outputFullPath = Resolve-RepoPath $OutputPath
$payloadFullPath = Resolve-RepoPath $PayloadPath

if (-not (Test-Path -LiteralPath $seedFullPath)) {
    throw "Seed file not found: $seedFullPath"
}

$seedText = Get-Content -LiteralPath $seedFullPath -Raw
try {
    $seed = $seedText | ConvertFrom-Json -ErrorAction Stop
} catch {
    throw "Seed file is not valid JSON: $seedFullPath`n$($_.Exception.Message)"
}

$collections = @(
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
    'attendees',
    'agendaItems',
    'papers',
    'minutes',
    'transcripts',
    'aiBuilderPrompts',
    'aiBuilderRuns',
    'decisions',
    'actions',
    'accessRules',
    'integrationBindings'
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
    'integrationBindings'
)

foreach ($collection in $requiredCollections) {
    if (@(Get-SeedItems -Seed $seed -Collection $collection).Count -lt 1) {
        throw "Seed file must contain at least one record in '$collection'."
    }
}

$governanceBodies = Get-SeedItems -Seed $seed -Collection 'governanceBodies'
$committees = Get-SeedItems -Seed $seed -Collection 'committees'
$termsOfReference = Get-SeedItems -Seed $seed -Collection 'termsOfReference'
$members = Get-SeedItems -Seed $seed -Collection 'members'
$orgRelationships = Get-SeedItems -Seed $seed -Collection 'orgRelationships'
$featureDefinitions = Get-SeedItems -Seed $seed -Collection 'featureDefinitions'
$meetingSeriesFeatureDefaults = Get-SeedItems -Seed $seed -Collection 'meetingSeriesFeatureDefaults'
$meetingFeatureOverrides = Get-SeedItems -Seed $seed -Collection 'meetingFeatureOverrides'
$meetingSeries = Get-SeedItems -Seed $seed -Collection 'meetingSeries'
$meetings = Get-SeedItems -Seed $seed -Collection 'meetings'
$attendees = Get-SeedItems -Seed $seed -Collection 'attendees'
$agendaItems = Get-SeedItems -Seed $seed -Collection 'agendaItems'
$papers = Get-SeedItems -Seed $seed -Collection 'papers'
$minutes = Get-SeedItems -Seed $seed -Collection 'minutes'
$transcripts = Get-SeedItems -Seed $seed -Collection 'transcripts'
$aiBuilderPrompts = Get-SeedItems -Seed $seed -Collection 'aiBuilderPrompts'
$aiBuilderRuns = Get-SeedItems -Seed $seed -Collection 'aiBuilderRuns'
$decisions = Get-SeedItems -Seed $seed -Collection 'decisions'
$actions = Get-SeedItems -Seed $seed -Collection 'actions'
$accessRules = Get-SeedItems -Seed $seed -Collection 'accessRules'
$integrationBindings = Get-SeedItems -Seed $seed -Collection 'integrationBindings'

$governanceBodyIndex = New-SeedIndex -Items $governanceBodies -KeyProperty 'key' -Label 'governance body'
$committeeIndex = New-SeedIndex -Items $committees -KeyProperty 'key' -Label 'committee'
$governanceBodyOrCommitteeIndex = @{}
foreach ($key in $governanceBodyIndex.Keys) { $governanceBodyOrCommitteeIndex[$key] = $governanceBodyIndex[$key] }
foreach ($key in $committeeIndex.Keys) { $governanceBodyOrCommitteeIndex[$key] = $committeeIndex[$key] }
$torIndex = New-SeedIndex -Items $termsOfReference -KeyProperty 'key' -Label 'terms of reference'
$personIndex = New-SeedIndex -Items $members -KeyProperty 'key' -Label 'member/person'
$personMailIndex = New-SeedIndex -Items $members -KeyProperty 'mail' -Label 'member/person mail'
$featureDefinitionIndex = New-SeedIndex -Items $featureDefinitions -KeyProperty 'key' -Label 'feature definition'
$meetingSeriesFeatureDefaultIndex = New-SeedIndex -Items $meetingSeriesFeatureDefaults -KeyProperty 'key' -Label 'meeting series feature default'
$meetingFeatureOverrideIndex = New-SeedIndex -Items $meetingFeatureOverrides -KeyProperty 'key' -Label 'meeting feature override'
$seriesIndex = New-SeedIndex -Items $meetingSeries -KeyProperty 'key' -Label 'meeting series'
$meetingIndex = New-SeedIndex -Items $meetings -KeyProperty 'key' -Label 'meeting'
$attendeeIndex = New-SeedIndex -Items $attendees -KeyProperty 'key' -Label 'attendee'
$agendaIndex = New-SeedIndex -Items $agendaItems -KeyProperty 'key' -Label 'agenda item'
$paperIndex = New-SeedIndex -Items $papers -KeyProperty 'key' -Label 'paper'
$minutesIndex = New-SeedIndex -Items $minutes -KeyProperty 'key' -Label 'minutes'
$transcriptIndex = New-SeedIndex -Items $transcripts -KeyProperty 'key' -Label 'transcript'
$aiBuilderPromptIndex = New-SeedIndex -Items $aiBuilderPrompts -KeyProperty 'key' -Label 'AI Builder prompt'
$aiBuilderRunIndex = New-SeedIndex -Items $aiBuilderRuns -KeyProperty 'key' -Label 'AI Builder run'
$decisionIndex = New-SeedIndex -Items $decisions -KeyProperty 'key' -Label 'decision'
$actionIndex = New-SeedIndex -Items $actions -KeyProperty 'key' -Label 'action'
$accessRuleIndex = New-SeedIndex -Items $accessRules -KeyProperty 'key' -Label 'access rule'

foreach ($item in $governanceBodies) {
    if ($item.ownerPersonKey) {
        Assert-ReferenceExists -Value (Resolve-PersonMail -PersonKey $item.ownerPersonKey -PeopleByKey $personIndex) -Index $personMailIndex -ReferenceName 'person mail' -Context "governance body '$($item.key)' owner"
    }
}

foreach ($item in $committees) {
    Assert-ReferenceExists -Value $item.governanceBodyKey -Index $governanceBodyOrCommitteeIndex -ReferenceName 'governance body or committee' -Context "committee '$($item.key)'"
    if ($item.ownerPersonKey) {
        Assert-ReferenceExists -Value (Resolve-PersonMail -PersonKey $item.ownerPersonKey -PeopleByKey $personIndex) -Index $personMailIndex -ReferenceName 'person mail' -Context "committee '$($item.key)' owner"
    }
}

foreach ($item in $termsOfReference) {
    Assert-ReferenceExists -Value $item.committeeKey -Index $committeeIndex -ReferenceName 'committee' -Context "terms of reference '$($item.key)'"
}

foreach ($item in $members) {
    Assert-ReferenceExists -Value $item.committeeKey -Index $committeeIndex -ReferenceName 'committee' -Context "member '$($item.key)'"
}

foreach ($item in $orgRelationships) {
    Assert-OptionalReferenceExists -Value $item.sourcePersonKey -Index $personIndex -ReferenceName 'person' -Context "org relationship '$($item.key)' source"
    Assert-OptionalReferenceExists -Value $item.targetPersonKey -Index $personIndex -ReferenceName 'person' -Context "org relationship '$($item.key)' target"
}

foreach ($item in $featureDefinitions) {
    if ([string]::IsNullOrWhiteSpace($item.featureCode)) {
        throw "feature definition '$($item.key)' is missing required property 'featureCode'."
    }
    if ([string]::IsNullOrWhiteSpace($item.name)) {
        throw "feature definition '$($item.key)' is missing required property 'name'."
    }
}

foreach ($item in $meetingSeriesFeatureDefaults) {
    Assert-ReferenceExists -Value $item.seriesKey -Index $seriesIndex -ReferenceName 'meeting series' -Context "meeting series feature default '$($item.key)'"
    Assert-ReferenceExists -Value $item.featureDefinitionKey -Index $featureDefinitionIndex -ReferenceName 'feature definition' -Context "meeting series feature default '$($item.key)'"
}

foreach ($item in $meetingFeatureOverrides) {
    Assert-ReferenceExists -Value $item.meetingKey -Index $meetingIndex -ReferenceName 'meeting' -Context "meeting feature override '$($item.key)'"
    Assert-ReferenceExists -Value $item.featureDefinitionKey -Index $featureDefinitionIndex -ReferenceName 'feature definition' -Context "meeting feature override '$($item.key)'"
}

foreach ($item in $meetingSeries) {
    Assert-ReferenceExists -Value $item.committeeKey -Index $committeeIndex -ReferenceName 'committee' -Context "meeting series '$($item.key)'"
    if ($item.seriesOwnerPersonKey) {
        Assert-ReferenceExists -Value (Resolve-PersonMail -PersonKey $item.seriesOwnerPersonKey -PeopleByKey $personIndex) -Index $personMailIndex -ReferenceName 'person mail' -Context "meeting series '$($item.key)' owner"
    }
}

foreach ($item in $meetings) {
    Assert-ReferenceExists -Value $item.seriesKey -Index $seriesIndex -ReferenceName 'meeting series' -Context "meeting '$($item.key)'"
    if ($item.chairPersonKey) {
        Assert-ReferenceExists -Value (Resolve-PersonMail -PersonKey $item.chairPersonKey -PeopleByKey $personIndex) -Index $personMailIndex -ReferenceName 'person mail' -Context "meeting '$($item.key)' chair"
    }
}

foreach ($item in $attendees) {
    Assert-ReferenceExists -Value $item.meetingKey -Index $meetingIndex -ReferenceName 'meeting' -Context "attendee '$($item.key)'"
    if ($item.personKey) {
        Assert-ReferenceExists -Value (Resolve-PersonMail -PersonKey $item.personKey -PeopleByKey $personIndex) -Index $personMailIndex -ReferenceName 'person mail' -Context "attendee '$($item.key)' person"
    }
    if ([string]::IsNullOrWhiteSpace([string]$item.displayName)) {
        throw "attendee '$($item.key)' is missing required property 'displayName'."
    }
}

foreach ($item in $agendaItems) {
    Assert-ReferenceExists -Value $item.meetingKey -Index $meetingIndex -ReferenceName 'meeting' -Context "agenda item '$($item.key)'"
    if ($item.presenterPersonKey) {
        Assert-ReferenceExists -Value (Resolve-PersonMail -PersonKey $item.presenterPersonKey -PeopleByKey $personIndex) -Index $personMailIndex -ReferenceName 'person mail' -Context "agenda item '$($item.key)' presenter"
    }
}

foreach ($item in $papers) {
    Assert-ReferenceExists -Value $item.meetingKey -Index $meetingIndex -ReferenceName 'meeting' -Context "paper '$($item.key)'"
    Assert-OptionalReferenceExists -Value $item.agendaItemKey -Index $agendaIndex -ReferenceName 'agenda item' -Context "paper '$($item.key)'"
    if ($item.ownerPersonKey) {
        Assert-ReferenceExists -Value (Resolve-PersonMail -PersonKey $item.ownerPersonKey -PeopleByKey $personIndex) -Index $personMailIndex -ReferenceName 'person mail' -Context "paper '$($item.key)' owner"
    }
}

foreach ($item in $minutes) {
    Assert-ReferenceExists -Value $item.meetingKey -Index $meetingIndex -ReferenceName 'meeting' -Context "minutes '$($item.key)'"
    if ($item.approvedByPersonKey) {
        Assert-ReferenceExists -Value (Resolve-PersonMail -PersonKey $item.approvedByPersonKey -PeopleByKey $personIndex) -Index $personMailIndex -ReferenceName 'person mail' -Context "minutes '$($item.key)' approver"
    }
}

foreach ($item in $transcripts) {
    Assert-ReferenceExists -Value $item.meetingKey -Index $meetingIndex -ReferenceName 'meeting' -Context "transcript '$($item.key)'"
}

foreach ($item in $aiBuilderPrompts) {
    if ([string]::IsNullOrWhiteSpace([string]$item.promptCode) -and [string]::IsNullOrWhiteSpace([string]$item.promptName)) {
        throw "AI Builder prompt '$($item.key)' is missing required property 'promptCode' or 'promptName'."
    }
}

foreach ($item in $aiBuilderRuns) {
    Assert-ReferenceExists -Value $item.meetingKey -Index $meetingIndex -ReferenceName 'meeting' -Context "AI Builder run '$($item.key)'"
    Assert-ReferenceExists -Value $item.promptKey -Index $aiBuilderPromptIndex -ReferenceName 'AI Builder prompt' -Context "AI Builder run '$($item.key)'"
    Assert-OptionalReferenceExists -Value $item.transcriptKey -Index $transcriptIndex -ReferenceName 'transcript' -Context "AI Builder run '$($item.key)'"
    Assert-OptionalReferenceExists -Value $item.minutesKey -Index $minutesIndex -ReferenceName 'minutes' -Context "AI Builder run '$($item.key)'"
    Assert-OptionalReferenceExists -Value $item.paperKey -Index $paperIndex -ReferenceName 'paper' -Context "AI Builder run '$($item.key)'"
    if ($item.reviewerPersonKey) {
        Assert-ReferenceExists -Value (Resolve-PersonMail -PersonKey $item.reviewerPersonKey -PeopleByKey $personIndex) -Index $personMailIndex -ReferenceName 'person mail' -Context "AI Builder run '$($item.key)' reviewer"
    }
}

foreach ($item in $decisions) {
    Assert-ReferenceExists -Value $item.meetingKey -Index $meetingIndex -ReferenceName 'meeting' -Context "decision '$($item.key)'"
    Assert-OptionalReferenceExists -Value $item.agendaItemKey -Index $agendaIndex -ReferenceName 'agenda item' -Context "decision '$($item.key)'"
    if ($item.ownerPersonKey) {
        Assert-ReferenceExists -Value (Resolve-PersonMail -PersonKey $item.ownerPersonKey -PeopleByKey $personIndex) -Index $personMailIndex -ReferenceName 'person mail' -Context "decision '$($item.key)' owner"
    }
}

foreach ($item in $actions) {
    Assert-ReferenceExists -Value $item.meetingKey -Index $meetingIndex -ReferenceName 'meeting' -Context "action '$($item.key)'"
    Assert-OptionalReferenceExists -Value $item.decisionKey -Index $decisionIndex -ReferenceName 'decision' -Context "action '$($item.key)'"
    Assert-OptionalReferenceExists -Value $item.agendaItemKey -Index $agendaIndex -ReferenceName 'agenda item' -Context "action '$($item.key)'"
    if ($item.ownerPersonKey) {
        Assert-ReferenceExists -Value (Resolve-PersonMail -PersonKey $item.ownerPersonKey -PeopleByKey $personIndex) -Index $personMailIndex -ReferenceName 'person mail' -Context "action '$($item.key)' owner"
    }
}

foreach ($item in $accessRules) {
    Assert-OptionalReferenceExists -Value $item.governanceBodyKey -Index $governanceBodyIndex -ReferenceName 'governance body' -Context "access rule '$($item.key)'"
    Assert-OptionalReferenceExists -Value $item.meetingKey -Index $meetingIndex -ReferenceName 'meeting' -Context "access rule '$($item.key)'"
    Assert-OptionalReferenceExists -Value $item.paperKey -Index $paperIndex -ReferenceName 'paper' -Context "access rule '$($item.key)'"
}

$counts = [ordered]@{}
foreach ($collection in $collections) {
    $counts[$collection] = @(Get-SeedItems -Seed $seed -Collection $collection).Count
}

$steps = New-Object System.Collections.Generic.List[object]

$lookupTargets = @{
    dft_parentgovernancebodyid = [pscustomobject]@{ table = 'dft_governancebody'; naturalKeyField = 'dft_bodycode' }
    dft_ownerpersonid = [pscustomobject]@{ table = 'dft_orgperson'; naturalKeyField = 'dft_workemail' }
    dft_governancebodyid = [pscustomobject]@{ table = 'dft_governancebody'; naturalKeyField = 'dft_bodycode' }
    dft_featuredefinitionid = [pscustomobject]@{ table = 'dft_featuredefinition'; naturalKeyField = 'dft_featurecode' }
    dft_orgpersonid = [pscustomobject]@{ table = 'dft_orgperson'; naturalKeyField = 'dft_workemail' }
    dft_sourcepersonid = [pscustomobject]@{ table = 'dft_orgperson'; naturalKeyField = 'dft_workemail' }
    dft_targetpersonid = [pscustomobject]@{ table = 'dft_orgperson'; naturalKeyField = 'dft_workemail' }
    dft_seriesownerid = [pscustomobject]@{ table = 'dft_orgperson'; naturalKeyField = 'dft_workemail' }
    dft_meetingseriesid = [pscustomobject]@{ table = 'dft_meetingseries'; naturalKeyField = 'dft_seriescode' }
    dft_chairpersonid = [pscustomobject]@{ table = 'dft_orgperson'; naturalKeyField = 'dft_workemail' }
    dft_meetingid = [pscustomobject]@{ table = 'dft_meeting'; naturalKeyField = 'dft_externalcalendarid' }
    dft_presenterid = [pscustomobject]@{ table = 'dft_orgperson'; naturalKeyField = 'dft_workemail' }
    dft_agendaitemid = [pscustomobject]@{ table = 'dft_agendaitem'; naturalKeyField = 'dft_name' }
    dft_ownerid = [pscustomobject]@{ table = 'dft_orgperson'; naturalKeyField = 'dft_workemail' }
    dft_approvedbyid = [pscustomobject]@{ table = 'dft_orgperson'; naturalKeyField = 'dft_workemail' }
    dft_decisionid = [pscustomobject]@{ table = 'dft_decision'; naturalKeyField = 'dft_decisionnumber' }
    dft_paperid = [pscustomobject]@{ table = 'dft_paper'; naturalKeyField = 'dft_documentid' }
    dft_aibuilderpromptid = [pscustomobject]@{ table = 'dft_aibuilderprompt'; naturalKeyField = 'dft_promptcode' }
    dft_minutesid = [pscustomobject]@{ table = 'dft_minutes'; naturalKeyField = 'dft_name' }
    dft_transcriptid = [pscustomobject]@{ table = 'dft_transcript'; naturalKeyField = 'dft_name' }
    dft_reviewerid = [pscustomobject]@{ table = 'dft_orgperson'; naturalKeyField = 'dft_workemail' }
}

foreach ($item in $governanceBodies) {
    $lookups = @{}
    if ($item.ownerPersonKey) {
        $lookups.dft_ownerpersonid = Resolve-PersonMail -PersonKey $item.ownerPersonKey -PeopleByKey $personIndex
    }

    $steps.Add((New-UpsertStep -Table 'dft_governancebody' -SourceCollection 'governanceBodies' -SourceKey $item.key -NaturalKey $item.key -FieldMap @{
        dft_name = $item.name
        dft_bodycode = $item.key
        dft_bodytype = $item.type
        dft_status = $item.status
        dft_description = $item.purpose
        dft_defaultclassification = $item.defaultClassification
    } -Lookups $lookups))
}

foreach ($item in $committees) {
    $lookups = @{
        dft_parentgovernancebodyid = $item.governanceBodyKey
    }
    if ($item.ownerPersonKey) {
        $lookups.dft_ownerpersonid = Resolve-PersonMail -PersonKey $item.ownerPersonKey -PeopleByKey $personIndex
    }

    $steps.Add((New-UpsertStep -Table 'dft_governancebody' -SourceCollection 'committees' -SourceKey $item.key -NaturalKey $item.key -FieldMap @{
        dft_name = $item.name
        dft_bodycode = $item.key
        dft_bodytype = $item.type
        dft_status = $item.status
        dft_defaultclassification = $item.classification
        dft_quorumnumber = $item.quorumNumber
        dft_meetingcadence = $item.meetingCadence
    } -Lookups $lookups))
}

foreach ($item in $termsOfReference) {
    $steps.Add((New-UpsertStep -Table 'dft_termsofreference' -SourceCollection 'termsOfReference' -SourceKey $item.key -NaturalKey ("{0}:{1}" -f $item.committeeKey, $item.version) -FieldMap @{
        dft_name = $item.title
        dft_versionnumber = $item.version
        dft_effectivefrom = $item.effectiveFrom
        dft_reviewdueon = $item.reviewDate
        dft_authority = $item.authority
        dft_status = $item.status
        dft_quorumnumber = $item.quorumNumber
        dft_quorumdenominator = $item.quorumDenominator
        dft_approvalstatus = $item.approvalStatus
        dft_approvedon = $item.approvedOn
    } -Lookups @{
        dft_governancebodyid = $item.committeeKey
    }))
}

foreach ($item in $members) {
    $personMail = Resolve-PersonMail -PersonKey $item.key -PeopleByKey $personIndex
    $steps.Add((New-UpsertStep -Table 'dft_orgperson' -SourceCollection 'members' -SourceKey $item.key -NaturalKey $item.mail -FieldMap @{
        dft_name = $item.displayName
        dft_aadobjectid = $item.key
        dft_workemail = $item.mail
        dft_userprincipalname = $item.mail
        dft_jobtitle = $item.jobTitle
        dft_department = $item.department
        dft_manageremail = $item.managerMail
        dft_lastsyncedon = $item.lastSyncedOn
    }))

    $membershipFieldMap = @{
        dft_name = ("{0} - {1}" -f $item.displayName, $item.role)
        dft_membershiprole = $item.role
        dft_startdate = $item.startDate
        dft_enddate = $item.endDate
        dft_membershipstatus = $item.membershipStatus
        dft_votingrights = $item.votingRights
        dft_isalternate = $item.isAlternate
    }
    if ($item.alternateForKey) {
        $membershipFieldMap.dft_isalternate = $true
    }

    $steps.Add((New-UpsertStep -Table 'dft_committeemembership' -SourceCollection 'members' -SourceKey $item.key -NaturalKey ("{0}:{1}" -f $item.committeeKey, $item.mail) -FieldMap $membershipFieldMap -Lookups @{
        dft_governancebodyid = $item.committeeKey
        dft_orgpersonid = $personMail
    }))
}

foreach ($item in $orgRelationships) {
    $steps.Add((New-UpsertStep -Table 'dft_orgrelationship' -SourceCollection 'orgRelationships' -SourceKey $item.key -NaturalKey ("{0}:{1}:{2}" -f $item.sourcePersonKey, $item.targetPersonKey, $item.relationshipType) -FieldMap @{
        dft_name = $item.key
        dft_relationshiptype = $item.relationshipType
        dft_effectivefrom = $item.effectiveFrom
        dft_effectiveto = $item.effectiveTo
        dft_source = $item.source
    } -Lookups @{
        dft_sourcepersonid = Resolve-PersonMail -PersonKey $item.sourcePersonKey -PeopleByKey $personIndex
        dft_targetpersonid = Resolve-PersonMail -PersonKey $item.targetPersonKey -PeopleByKey $personIndex
    }))
}

foreach ($item in $featureDefinitions) {
    $steps.Add((New-UpsertStep -Table 'dft_featuredefinition' -SourceCollection 'featureDefinitions' -SourceKey $item.key -NaturalKey $item.featureCode -FieldMap @{
        dft_name = $item.name
        dft_featurecode = $item.featureCode
        dft_description = $item.description
        dft_featurecategory = $item.category
        dft_defaultenabled = $item.defaultEnabled
        dft_scopetype = $item.scope
        dft_status = $item.status
        dft_sortorder = $item.sortOrder
    }))
}

foreach ($item in $meetingSeriesFeatureDefaults) {
    $steps.Add((New-UpsertStep -Table 'dft_meetingseriesfeature' -SourceCollection 'meetingSeriesFeatureDefaults' -SourceKey $item.key -NaturalKey ("{0}:{1}" -f $item.seriesKey, $item.featureDefinitionKey) -FieldMap @{
        dft_name = $item.name
        dft_enabled = $item.enabled
        dft_rationale = $item.reason
        dft_status = $item.status
        dft_lastsyncedon = $item.lastSyncedOn
    } -Lookups @{
        dft_meetingseriesid = $item.seriesKey
        dft_featuredefinitionid = $item.featureDefinitionKey
    }))
}

foreach ($item in $meetingSeries) {
    $lookups = @{
        dft_governancebodyid = $item.committeeKey
    }
    if ($item.seriesOwnerPersonKey) {
        $lookups.dft_seriesownerid = Resolve-PersonMail -PersonKey $item.seriesOwnerPersonKey -PeopleByKey $personIndex
    }

    $steps.Add((New-UpsertStep -Table 'dft_meetingseries' -SourceCollection 'meetingSeries' -SourceKey $item.key -NaturalKey $item.key -FieldMap @{
        dft_name = $item.name
        dft_seriescode = $item.key
        dft_meetingcadence = $item.cadence
        dft_recurrencerule = $item.recurrenceRule
        dft_defaultlocation = $item.defaultLocation
        dft_defaultchannelid = $item.defaultChannelId
        dft_defaultteamid = $item.defaultTeamId
        dft_seriesstatus = $item.seriesStatus
    } -Lookups $lookups))
}

foreach ($item in $meetings) {
    $lookups = @{
        dft_meetingseriesid = $item.seriesKey
    }
    if ($item.chairPersonKey) {
        $lookups.dft_chairpersonid = Resolve-PersonMail -PersonKey $item.chairPersonKey -PeopleByKey $personIndex
    }

    $steps.Add((New-UpsertStep -Table 'dft_meeting' -SourceCollection 'meetings' -SourceKey $item.key -NaturalKey ($(if ($item.externalCalendarId) { $item.externalCalendarId } else { $item.key })) -FieldMap @{
        dft_name = $item.title
        dft_subject = $item.title
        dft_meetingnumber = $item.meetingNumber
        dft_startdatetime = $item.scheduledStartUtc
        dft_enddatetime = $item.scheduledEndUtc
        dft_scheduledstartutc = $item.scheduledStartUtc
        dft_scheduledendutc = $item.scheduledEndUtc
        dft_actualstartutc = $item.actualStartUtc
        dft_actualendutc = $item.actualEndUtc
        dft_meetingstatus = $item.status
        dft_externalcalendarid = $item.externalCalendarId
        dft_outlookeventid = $item.externalCalendarId
        dft_externalteamsid = $item.externalTeamsId
        dft_teamsjoinurl = $item.teamsJoinUrl
        dft_location = $item.location
        dft_inviterequested = $true
        dft_invitesyncstate = 'queued'
        dft_invitesynchash = $item.externalCalendarId
        dft_requiredattendees = (($members | Where-Object { $_.votingRights } | ForEach-Object { $_.mail }) -join ';')
        dft_optionalattendees = (($members | Where-Object { -not $_.votingRights } | ForEach-Object { $_.mail }) -join ';')
        dft_classification = $item.classification
    } -Lookups $lookups))
}

foreach ($item in $attendees) {
    $lookups = @{
        dft_meetingid = $item.meetingKey
    }
    $personMail = Resolve-PersonMail -PersonKey $item.personKey -PeopleByKey $personIndex
    if ($personMail) {
        $lookups.dft_orgpersonid = $personMail
    }

    $emailAddress = if ($item.emailAddress) { $item.emailAddress } else { $personMail }
    $attendeeRole = if ($item.attendeeRole) { $item.attendeeRole } elseif ($item.role) { $item.role } else { 'member' }

    $steps.Add((New-UpsertStep -Table 'dft_attendee' -SourceCollection 'attendees' -SourceKey $item.key -NaturalKey ("{0}:{1}" -f $item.meetingKey, $(if ($emailAddress) { $emailAddress } else { $item.key })) -FieldMap @{
        dft_name = $item.displayName
        dft_displayname = $item.displayName
        dft_emailaddress = $emailAddress
        dft_response = $item.response
        dft_attendancestatus = $item.attendanceStatus
        dft_attendeerole = $attendeeRole
        dft_isproxy = if ($null -ne $item.isProxy) { $item.isProxy } else { $false }
        dft_votingeligible = if ($null -ne $item.votingEligible) { $item.votingEligible } else { $false }
        dft_invitedon = $item.invitedOn
        dft_responseupdatedon = $item.responseUpdatedOn
        dft_outlookattendeeid = $item.outlookAttendeeId
    } -Lookups $lookups))
}

foreach ($item in $meetingFeatureOverrides) {
    $steps.Add((New-UpsertStep -Table 'dft_meetingfeatureoverride' -SourceCollection 'meetingFeatureOverrides' -SourceKey $item.key -NaturalKey ("{0}:{1}" -f $item.meetingKey, $item.featureDefinitionKey) -FieldMap @{
        dft_name = $item.name
        dft_enabled = $item.enabled
        dft_rationale = $item.reason
        dft_status = $item.status
        dft_lastsyncedon = $item.lastSyncedOn
    } -Lookups @{
        dft_meetingid = $item.meetingKey
        dft_featuredefinitionid = $item.featureDefinitionKey
    }))
}

foreach ($item in $agendaItems) {
    $lookups = @{
        dft_meetingid = $item.meetingKey
    }
    if ($item.presenterPersonKey) {
        $lookups.dft_presenterid = Resolve-PersonMail -PersonKey $item.presenterPersonKey -PeopleByKey $personIndex
    }

    $steps.Add((New-UpsertStep -Table 'dft_agendaitem' -SourceCollection 'agendaItems' -SourceKey $item.key -NaturalKey ("{0}:{1}" -f $item.meetingKey, $item.sequence) -FieldMap @{
        dft_name = $item.title
        dft_sequence = $item.sequence
        dft_agendatitle = $item.title
        dft_purpose = $item.purpose
        dft_timeminutes = $item.durationMinutes
        dft_minutetext = $item.minuteText
        dft_decisionsought = $item.decisionSought
        dft_status = $item.status
    } -Lookups $lookups))
}

foreach ($item in $papers) {
    $lookups = @{
        dft_meetingid = $item.meetingKey
    }
    if ($item.agendaItemKey) {
        $lookups.dft_agendaitemid = $item.agendaItemKey
    }
    if ($item.ownerPersonKey) {
        $lookups.dft_ownerid = Resolve-PersonMail -PersonKey $item.ownerPersonKey -PeopleByKey $personIndex
    }

    $steps.Add((New-UpsertStep -Table 'dft_paper' -SourceCollection 'papers' -SourceKey $item.key -NaturalKey ($(if ($item.documentId) { $item.documentId } else { $item.key })) -FieldMap @{
        dft_name = $item.title
        dft_documentid = $item.documentId
        dft_documentsource = $item.documentSource
        dft_classification = $item.classification
        dft_paperstatus = $item.paperStatus
        dft_sharepointurl = $item.sharepointUrl
        dft_sharepointsiteid = $item.sharepointSiteId
        dft_sharepointdriveid = $item.sharepointDriveId
        dft_sharepointitemid = $item.sharepointItemId
    } -Lookups $lookups))
}

foreach ($item in $minutes) {
    $lookups = @{
        dft_meetingid = $item.meetingKey
    }
    if ($item.approvedByPersonKey) {
        $lookups.dft_approvedbyid = Resolve-PersonMail -PersonKey $item.approvedByPersonKey -PeopleByKey $personIndex
    }

    $steps.Add((New-UpsertStep -Table 'dft_minutes' -SourceCollection 'minutes' -SourceKey $item.key -NaturalKey ("{0}:{1}" -f $item.meetingKey, $item.version) -FieldMap @{
        dft_name = $item.title
        dft_versionnumber = $item.version
        dft_status = $item.status
        dft_drafttext = $item.draftText
        dft_approvedon = $item.approvedOn
        dft_publishedon = $item.publishedOn
        dft_publishedurl = $item.publishedUrl
        dft_sharepointsiteid = $item.sharepointSiteId
        dft_sharepointdriveid = $item.sharepointDriveId
        dft_sharepointitemid = $item.sharepointItemId
        dft_classification = $item.classification
    } -Lookups $lookups))
}

foreach ($item in $transcripts) {
    $steps.Add((New-UpsertStep -Table 'dft_transcript' -SourceCollection 'transcripts' -SourceKey $item.key -NaturalKey $item.meetingKey -FieldMap @{
        dft_name = $item.title
        dft_source = $item.source
        dft_transcriptstatus = if ($item.status) { $item.status } else { $item.transcriptStatus }
        dft_classification = $item.classification
        dft_transcripttext = $item.transcriptText
        dft_fileurl = $item.fileUrl
        dft_sharepointsiteid = $item.sharepointSiteId
        dft_sharepointdriveid = $item.sharepointDriveId
        dft_sharepointitemid = $item.sharepointItemId
        dft_sourcehash = $item.sourceHash
        dft_processedon = $item.processedOn
        dft_retentionstatus = $item.retentionStatus
    } -Lookups @{
        dft_meetingid = $item.meetingKey
    }))
}

foreach ($item in $aiBuilderPrompts) {
    $promptCode = if ($item.promptCode) { $item.promptCode } else { $item.key }
    $promptVersion = if ($item.versionNumber) { $item.versionNumber } else { $item.version }
    $promptPurpose = if ($item.promptPurpose) { $item.promptPurpose } else { $item.purpose }
    $inputType = if ($item.inputType) { $item.inputType } else { 'transcript' }
    $classificationLimit = if ($item.classificationLimit) { $item.classificationLimit } else { $item.classificationAllowance }

    $steps.Add((New-UpsertStep -Table 'dft_aibuilderprompt' -SourceCollection 'aiBuilderPrompts' -SourceKey $item.key -NaturalKey ("{0}:{1}" -f $promptCode, $promptVersion) -FieldMap @{
        dft_name = if ($item.promptName) { $item.promptName } else { $item.name }
        dft_promptcode = $promptCode
        dft_versionnumber = $promptVersion
        dft_purpose = $promptPurpose
        dft_inputtype = $inputType
        dft_outputschema = $item.outputSchema
        dft_approvalstatus = $item.approvalStatus
        dft_classificationlimit = $classificationLimit
        dft_prompttext = $item.promptText
    }))
}

foreach ($item in $aiBuilderRuns) {
    $lookups = @{
        dft_aibuilderpromptid = $(if ($aiBuilderPromptIndex.ContainsKey($item.promptKey)) {
            $promptItem = $aiBuilderPromptIndex[$item.promptKey]
            if ($promptItem.promptCode) { $promptItem.promptCode } else { $promptItem.key }
        } else {
            $item.promptKey
        })
        dft_meetingid = $item.meetingKey
    }
    if ($item.minutesKey) {
        $lookups.dft_minutesid = if ($minutesIndex.ContainsKey($item.minutesKey)) {
            $minutesIndex[$item.minutesKey].title
        } else {
            $item.minutesKey
        }
    }
    if ($item.transcriptKey) {
        $lookups.dft_transcriptid = if ($transcriptIndex.ContainsKey($item.transcriptKey)) {
            $transcriptIndex[$item.transcriptKey].title
        } else {
            $item.transcriptKey
        }
    }
    if ($item.paperKey) {
        $lookups.dft_paperid = if ($paperIndex.ContainsKey($item.paperKey)) {
            $paperItem = $paperIndex[$item.paperKey]
            if ($paperItem.documentId) { $paperItem.documentId } else { $paperItem.key }
        } else {
            $item.paperKey
        }
    }
    if ($item.reviewerPersonKey) {
        $lookups.dft_reviewerid = Resolve-PersonMail -PersonKey $item.reviewerPersonKey -PeopleByKey $personIndex
    }

    $steps.Add((New-UpsertStep -Table 'dft_aibuilderrun' -SourceCollection 'aiBuilderRuns' -SourceKey $item.key -NaturalKey ($(if ($item.correlationId) { $item.correlationId } else { $item.key })) -FieldMap @{
        dft_name = $item.name
        dft_runstatus = $item.runStatus
        dft_humanreviewstatus = $item.humanReviewStatus
        dft_outputtext = if ($item.outputText) { $item.outputText } else { $item.exceptionNotes }
        dft_inputhash = $item.inputHash
        dft_outputhash = $item.outputHash
        dft_reviewedon = $item.reviewedOn
        dft_reviewcomments = $item.reviewComments
        dft_correlationid = if ($item.correlationId) { $item.correlationId } else { $item.key }
    } -Lookups $lookups))
}

foreach ($item in $decisions) {
    $lookups = @{
        dft_meetingid = $item.meetingKey
    }
    if ($item.agendaItemKey) {
        $lookups.dft_agendaitemid = $item.agendaItemKey
    }
    if ($item.ownerPersonKey) {
        $lookups.dft_ownerid = Resolve-PersonMail -PersonKey $item.ownerPersonKey -PeopleByKey $personIndex
    }
    if ($item.governanceBodyKey) {
        $lookups.dft_governancebodyid = $item.governanceBodyKey
    }

    $steps.Add((New-UpsertStep -Table 'dft_decision' -SourceCollection 'decisions' -SourceKey $item.key -NaturalKey ($(if ($item.decisionNumber) { $item.decisionNumber } else { $item.key })) -FieldMap @{
        dft_name = $item.title
        dft_decisionnumber = $item.decisionNumber
        dft_decisiontext = $item.decisionText
        dft_authority = $item.authority
        dft_status = $item.status
        dft_effectiveon = $item.effectiveOn
        dft_reviewdueon = $item.reviewDueOn
    } -Lookups $lookups))
}

foreach ($item in $actions) {
    $lookups = @{
        dft_meetingid = $item.meetingKey
    }
    if ($item.decisionKey) {
        $lookups.dft_decisionid = $item.decisionKey
    }
    if ($item.agendaItemKey) {
        $lookups.dft_agendaitemid = $item.agendaItemKey
    }
    if ($item.ownerPersonKey) {
        $lookups.dft_ownerid = Resolve-PersonMail -PersonKey $item.ownerPersonKey -PeopleByKey $personIndex
    }
    if ($item.governanceBodyKey) {
        $lookups.dft_governancebodyid = $item.governanceBodyKey
    }

    $steps.Add((New-UpsertStep -Table 'dft_actionitem' -SourceCollection 'actions' -SourceKey $item.key -NaturalKey ($(if ($item.actionNumber) { $item.actionNumber } else { $item.key })) -FieldMap @{
        dft_name = $item.title
        dft_actionnumber = $item.actionNumber
        dft_actionsummary = $item.title
        dft_duedate = $item.dueDate
        dft_status = $item.status
        dft_priority = $item.priority
        dft_synctoplanner = (-not [string]::IsNullOrWhiteSpace([string]$item.plannerTaskId))
        dft_requiresplannersync = (-not [string]::IsNullOrWhiteSpace([string]$item.plannerTaskId))
        dft_plannersyncstatus = if ([string]::IsNullOrWhiteSpace([string]$item.plannerTaskId)) { 'skipped' } else { 'queued' }
        dft_lastsynchash = $item.plannerTaskId
        dft_plannertaskid = $item.plannerTaskId
        dft_completionevidence = $item.completionEvidence
    } -Lookups $lookups))
}

foreach ($item in $accessRules) {
    $lookups = @{}
    if ($item.governanceBodyKey) {
        $lookups.dft_governancebodyid = $item.governanceBodyKey
    }
    if ($item.meetingKey) {
        $lookups.dft_meetingid = $item.meetingKey
    }
    if ($item.paperKey) {
        $lookups.dft_paperid = $item.paperKey
    }

    $steps.Add((New-UpsertStep -Table 'dft_accessrule' -SourceCollection 'accessRules' -SourceKey $item.key -NaturalKey ("{0}:{1}:{2}" -f $item.scope, $item.classification, $item.ruleType) -FieldMap @{
        dft_name = $item.key
        dft_scope = $item.scope
        dft_ruletype = $item.ruleType
        dft_classification = $item.classification
        dft_rulesummary = $item.ruleSummary
    } -Lookups $lookups))
}

$bindingHints = @(
    $integrationBindings | ForEach-Object {
        [pscustomobject]@{
            key = $_.key
            targetSystem = $_.targetSystem
            settingName = $_.settingName
            exampleValue = $_.exampleValue
            purpose = $_.purpose
            targetRecordKey = $_.targetRecordKey
        }
    }
)

$plan = [pscustomobject]@{
    generatedOn = (Get-Date).ToString('o')
    seedPath = $seedFullPath
    outputPath = $outputFullPath
    dryRun = [bool]$DryRun
    counts = $counts
    stepCount = $steps.Count
    bindingHintCount = $bindingHints.Count
    bindingHints = $bindingHints
    steps = $steps
    note = 'This is a deterministic import plan. Use it as the maker/API contract for Dataverse upserts; no live writes are performed by this helper.'
}

$payloadBundle = [pscustomobject]@{
    generatedOn = $plan.generatedOn
    seedPath = $seedFullPath
    payloadPath = $payloadFullPath
    payloadCount = $steps.Count
    mode = 'dataverse-upsert-payload-bundle'
    note = 'Payloads are ready for a governed Dataverse Web API/import implementation. Lookup bindings name target tables and natural keys, but this helper does not resolve GUIDs or write live records.'
    payloads = @(
        $steps | ForEach-Object {
            New-DataversePayload -Step $_ -LookupTargets $lookupTargets
        }
    )
}

if ($WritePayloads -and -not $DryRun) {
    $payloadDirectory = Split-Path -Parent $payloadFullPath
    New-Item -ItemType Directory -Path $payloadDirectory -Force | Out-Null
    $payloadBundle | ConvertTo-Json -Depth 14 | Set-Content -LiteralPath $payloadFullPath -Encoding UTF8
    Write-Host "Wrote Dataverse payload bundle: $payloadFullPath"
}

if ($Json) {
    if ($WritePayloads) {
        [pscustomobject]@{
            plan = $plan
            dataversePayloadBundle = $payloadBundle
        } | ConvertTo-Json -Depth 14
    } else {
        $plan | ConvertTo-Json -Depth 12
    }
    return
}

if ($DryRun) {
    Write-Host "Seed import dry run"
    Write-Host "Seed file: $seedFullPath"
    Write-Host "Planned upsert steps: $($steps.Count)"
    Write-Host "Binding hints: $($bindingHints.Count)"
    if ($WritePayloads) {
        Write-Host "Dataverse payloads: $($payloadBundle.payloadCount)"
    }
    foreach ($key in $counts.Keys) {
        Write-Host ("- {0}: {1}" -f $key, $counts[$key])
    }
    return $plan
}

$outputDirectory = Split-Path -Parent $outputFullPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$plan | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $outputFullPath -Encoding UTF8
Write-Host "Wrote seed import plan: $outputFullPath"
return $plan

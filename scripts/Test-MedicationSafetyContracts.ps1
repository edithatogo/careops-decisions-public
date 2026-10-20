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
        throw "Missing Medication Safety artifact: $RelativePath"
    }

    $content = Get-Content -LiteralPath $fullPath -Raw
    if ([string]::IsNullOrWhiteSpace($content)) {
        throw "Medication Safety artifact is empty: $RelativePath"
    }

    foreach ($pattern in $Patterns) {
        if ($content -notmatch [regex]::Escape($pattern)) {
            throw "$RelativePath does not reference required text: $pattern"
        }
    }

    return $content
}

function Assert-JsonFile {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $fullPath = Get-RepoPath $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw "Missing Medication Safety JSON artifact: $RelativePath"
    }

    return Get-Content -LiteralPath $fullPath -Raw | ConvertFrom-Json
}

Assert-FileContains -RelativePath 'docs/guides/decisions-workspace-quick-start.md' -Patterns @(
    'Add It To Teams',
    'Website',
    'Role-Based First Steps',
    'Chair',
    'Secretariat',
    'Administrator',
    'Sample Data vs. Real Data',
    'NSW Outlook, SharePoint, Teams, or user-provided exports',
    'Gmail is not a valid source',
    'Import-MedicationSafetySampleToDataverse.ps1 -WhatIfOnly',
    'Environment variables'
) | Out-Null

Assert-FileContains -RelativePath 'docs/implementation/decisions-feature-coverage.md' -Patterns @(
    'Medication Safety committee operating model',
    'Medication Safety synthetic seed data',
    'Medication Safety meeting workflow',
    'Medication Safety left navigation and home',
    'Medication Safety AI Builder prompt contract',
    'Implemented',
    'Scaffolded',
    'Blocked'
) | Out-Null

Assert-FileContains -RelativePath 'docs/architecture/ai-builder-minutes-pattern.md' -Patterns @(
    'Human Review Gate',
    'Audit Record',
    'Prompt Contract',
    'decisions_EnableAiBuilderMinutes',
    'decisions_AiBuilderPromptName',
    'dft_AIBuilderRun',
    'dft_AIBuilderPrompt',
    'dft_IntegrationLog',
    'No AI-generated content is official'
) | Out-Null

Assert-FileContains -RelativePath 'docs/architecture/ai-human-review-gates.md' -Patterns @(
    'Publication approval',
    'dft_AIBuilderRun',
    'dft_Minutes',
    'dft_Decision',
    'dft_ActionItem',
    'dft_IntegrationLog',
    'No AI-generated content can reach published minutes'
) | Out-Null

Assert-FileContains -RelativePath 'docs/operations/medication-safety-committee-workflows.md' -Patterns @(
    'NSW Outlook, SharePoint, Teams, or user-provided exported files',
    'Gmail is not a valid source',
    'Synthetic',
    'Outlook invite sync',
    'Attendees',
    'Agenda',
    'Papers',
    'Transcript',
    'minutes',
    'decisions',
    'actions',
    'Planner',
    'Teams',
    'SharePoint',
    'Terms of reference',
    'Access',
    'Reporting',
    'feature defaults',
    'Import-MedicationSafetySampleToDataverse.ps1 -WhatIfOnly'
) | Out-Null

Assert-FileContains -RelativePath 'scripts/Import-MedicationSafetySampleToDataverse.ps1' -Patterns @(
    'Assert-MedicationSafetySeedContract',
    'WhatIfOnly',
    'example.invalid',
    'Synthetic',
    'AI Builder',
    'integrationLogs',
    'Offline validation only'
) | Out-Null

Assert-FileContains -RelativePath 'conductor/tracks/track-23-medication-safety-operationalization_20260504/plan.md' -Patterns @(
    'environment-variable-manifest.json',
    'deployment-settings.sample.json',
    'source-controlled solution environment variable definitions',
    'full settings import',
    'NSW Outlook',
    'Stop using Gmail'
) | Out-Null

$seed = Assert-JsonFile -RelativePath 'powerplatform/src/seed/medication-safety-sample-data.json'
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
    'attendees',
    'transcripts',
    'accessRules',
    'integrationBindings',
    'integrationLogs',
    'aiBuilderPrompts',
    'aiBuilderRuns'
)

foreach ($collection in $requiredCollections) {
    if (-not ($seed.PSObject.Properties.Name -contains $collection)) {
        throw "Medication Safety seed is missing required collection: $collection"
    }
    if (@($seed.$collection).Count -lt 1) {
        throw "Medication Safety seed collection is empty: $collection"
    }
}

if ([string]$seed.sourceNotice -notmatch 'Synthetic') {
    throw 'Medication Safety seed must explicitly identify itself as synthetic.'
}

if ((@($seed.members | Where-Object { $_.role -eq 'chair' }).Count) -lt 1) {
    throw 'Medication Safety seed must include a chair member.'
}

if ((@($seed.members | Where-Object { $_.role -eq 'secretary' }).Count) -lt 1) {
    throw 'Medication Safety seed must include a secretariat member.'
}

foreach ($member in @($seed.members)) {
    if ([string]$member.mail -notmatch '@example\.invalid$') {
        throw "Medication Safety seed member '$($member.key)' must use an example.invalid mailbox."
    }
}

if ((Get-Content -LiteralPath (Get-RepoPath 'powerplatform/src/seed/medication-safety-sample-data.json') -Raw) -match 'gmail\.com') {
    throw 'Medication Safety seed must not contain Gmail addresses.'
}

if ((@($seed.minutes | Where-Object { $_.status -eq 'approved' }).Count) -lt 1) {
    throw 'Medication Safety seed must include approved minutes evidence.'
}

if ((@($seed.attendees | Where-Object { $_.attendeeRole -eq 'chair' }).Count) -lt 1) {
    throw 'Medication Safety seed must include a chair attendee row.'
}

if ((@($seed.integrationLogs | Where-Object { $_.status -eq 'skipped' }).Count) -lt 1) {
    throw 'Medication Safety seed must include skipped integration evidence for the real-data gate.'
}

$sampleValidation = & (Join-Path $repoRoot 'scripts/Import-MedicationSafetySampleToDataverse.ps1') -WhatIfOnly
if ($sampleValidation.validation -ne 'passed') {
    throw 'Medication Safety sample WhatIf validation did not return a passed result.'
}

Write-Host 'Medication Safety contract validation passed.'

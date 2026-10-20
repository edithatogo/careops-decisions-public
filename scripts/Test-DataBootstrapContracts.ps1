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
        throw "Missing data bootstrap artifact: $RelativePath"
    }

    $content = Get-Content -LiteralPath $fullPath -Raw
    if ([string]::IsNullOrWhiteSpace($content)) {
        throw "Data bootstrap artifact is empty: $RelativePath"
    }

    foreach ($pattern in $Patterns) {
        if ($content -notmatch [regex]::Escape($pattern)) {
            throw "$RelativePath does not reference required text: $pattern"
        }
    }

    return $content
}

function Get-JsonObject {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $fullPath = Get-RepoPath $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw "Missing fixture pack: $RelativePath"
    }

    return Get-Content -LiteralPath $fullPath -Raw | ConvertFrom-Json
}

$schemaText = Assert-FileContains -RelativePath 'powerplatform/src/schema/dataverse-tables.yaml' -Patterns @(
    'tables:',
    'logicalName: dft_GovernanceBody',
    'logicalName: dft_Minutes',
    'logicalName: dft_Paper',
    'logicalName: dft_Decision',
    'logicalName: dft_ActionItem'
)

$knownSchemaNames = [regex]::Matches($schemaText, 'logicalName:\s+(dft_[A-Za-z0-9_]+)') |
    ForEach-Object { $_.Groups[1].Value } |
    Sort-Object -Unique

$fixtureDoc = Assert-FileContains -RelativePath 'docs/operations/fixture-packs.md' -Patterns @(
    'Small (single committee, one meeting)',
    'Medium (multi-level hierarchy, multiple meetings)',
    'Edge-Case',
    'Record counts',
    'powerplatform/src/seed/fixture-small.json',
    'powerplatform/src/seed/fixture-medium.json',
    'powerplatform/src/seed/fixture-edge.json',
    'Import-SeedData.ps1',
    'SYNTHETIC'
)

$fixtureCollections = @(
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
    'integrationBindings'
)

$fixturePacks = @(
    @{ Path = 'powerplatform/src/seed/fixture-small.json'; Name = 'small' },
    @{ Path = 'powerplatform/src/seed/fixture-medium.json'; Name = 'medium' },
    @{ Path = 'powerplatform/src/seed/fixture-edge.json'; Name = 'edge' }
)

foreach ($fixturePack in $fixturePacks) {
    $fixture = Get-JsonObject -RelativePath $fixturePack.Path
    if ($fixture.fixturePack -ne $fixturePack.Name) {
        throw "$($fixturePack.Path) has fixturePack '$($fixture.fixturePack)', expected '$($fixturePack.Name)'."
    }

    if ($fixture.sourceNotice -notmatch 'SYNTHETIC') {
        throw "$($fixturePack.Path) must carry an explicit synthetic data notice."
    }

    foreach ($collectionName in $fixtureCollections) {
        $property = $fixture.PSObject.Properties[$collectionName]
        if ($null -eq $property) {
            throw "$($fixturePack.Path) is missing fixture collection '$collectionName'."
        }

        if ($property.Value -isnot [System.Array]) {
            throw "$($fixturePack.Path) collection '$collectionName' must be a JSON array."
        }
    }

    if ($fixtureDoc -notmatch [regex]::Escape($fixturePack.Path)) {
        throw "Fixture documentation does not reference $($fixturePack.Path)."
    }
}

$mappingText = Assert-FileContains -RelativePath 'docs/operations/import-mapping-templates.md' -Patterns @(
    'Committee Register',
    'Minutes Register',
    'Paper Register',
    'Decision Log',
    'Action Log',
    'Source Field',
    'Source Example',
    'Target Column',
    'Required',
    'Transformation',
    'dft_GovernanceBody',
    'dft_Minutes',
    'dft_Paper',
    'dft_Decision',
    'dft_ActionItem'
)

$mappedSchemaNames = [regex]::Matches($mappingText, '`(dft_[A-Za-z0-9_]+)`') |
    ForEach-Object { $_.Groups[1].Value } |
    Sort-Object -Unique

$missingNames = @($mappedSchemaNames | Where-Object { $_ -notin $knownSchemaNames })
if ($missingNames.Count -gt 0) {
    throw "Import mapping templates reference unknown Dataverse names: $($missingNames -join ', ')"
}

Assert-FileContains -RelativePath 'docs/operations/bootstrap-runbook.md' -Patterns @(
    'Provision Environment',
    'Configure Solution Publisher',
    'Build Schema',
    'Validate Schema',
    'Import Sample Data',
    'Validate Import Plan',
    'Smoke Test',
    'Data-Only Reseed',
    'Checkpoint-Based Restore'
) | Out-Null

Assert-FileContains -RelativePath 'docs/operations/schema-migration-policy.md' -Patterns @(
    'Safe Add',
    'Safe Rename',
    'Breaking Change',
    'requires explicit review',
    'Data loss assessment',
    'Import mapping templates have been updated',
    'Breaking changes must not be committed without an accompanying ADR'
) | Out-Null

Write-Host "Data bootstrap contract validation passed. ($($fixturePacks.Count) fixture packs, $($mappedSchemaNames.Count) mapped Dataverse names)"

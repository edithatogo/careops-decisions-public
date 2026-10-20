param(
    [string]$SchemaRoot = (Join-Path (Split-Path -Parent $PSScriptRoot) 'powerplatform/src/schema'),
    [string]$OutputPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'powerplatform/src/schema/schema-build-plan.json'),
    [switch]$ValidateOnly
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Get-FileText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Missing file: $Path"
    }

    return [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $Path))
}

function Get-OrderedJsonText {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Value
    )

    return ($Value | ConvertTo-Json -Depth 20)
}

function Normalize-JsonText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    return (($Text.Trim() -replace "`r`n", "`n") -replace "`r", "`n")
}

function Get-PlanFingerprint {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Plan
    )

    $tables = @(
        $Plan.tableCreationOrder | Sort-Object logicalName | ForEach-Object {
            [ordered]@{
                logicalName = $_.logicalName
                displayName = $_.displayName
                primaryField = $_.primaryField
                choiceDependencies = @($_.choiceDependencies | Sort-Object)
                lookupDependencies = @($_.lookupDependencies | Sort-Object)
            }
        }
    )

    $relationships = [ordered]@{}
    foreach ($phaseName in @('mvp', 'future')) {
        $relationships[$phaseName] = @(
            $Plan.relationshipPhases.$phaseName | Sort-Object name | ForEach-Object {
                [ordered]@{
                    name = $_.name
                    fromTable = $_.fromTable
                    fromColumn = $_.fromColumn
                    toTable = $_.toTable
                    type = $_.type
                    required = [bool]$_.required
                    deleteBehavior = $_.deleteBehavior
                }
            }
        )
    }

    return [ordered]@{
        publisherPrefix = $Plan.publisherPrefix
        schemaVersion = $Plan.schemaVersion
        sourceFiles = [ordered]@{
            tables = $Plan.sourceFiles.tables
            choices = $Plan.sourceFiles.choices
            relationships = $Plan.sourceFiles.relationships
        }
        tableCreationOrder = $tables
        relationshipPhases = $relationships
        choiceSets = @($Plan.choiceSets | Sort-Object)
    }
}

function Get-FirstDifference {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Expected,

        [Parameter(Mandatory = $true)]
        [string]$Actual
    )

    $expectedLines = @((Normalize-JsonText -Text $Expected) -split "`n")
    $actualLines = @((Normalize-JsonText -Text $Actual) -split "`n")
    $max = [Math]::Max($expectedLines.Count, $actualLines.Count)
    for ($i = 0; $i -lt $max; $i++) {
        $expectedLine = if ($i -lt $expectedLines.Count) { $expectedLines[$i] } else { '<missing>' }
        $actualLine = if ($i -lt $actualLines.Count) { $actualLines[$i] } else { '<missing>' }
        if ($expectedLine -ne $actualLine) {
            return "line $($i + 1): expected '$expectedLine' but generated '$actualLine'"
        }
    }

    return 'unknown fingerprint difference'
}

function Parse-YamlTables {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    $result = New-Object System.Collections.Generic.List[object]
    $matches = [regex]::Matches(
        $Text,
        '(?ms)^[^\S\r\n]{2}-[^\S\r\n]+logicalName:[^\S\r\n]*(?<logicalName>\S+)[^\S\r\n]*\r?\n(?<block>.*?)(?=^[^\S\r\n]{2}-[^\S\r\n]+logicalName:|\z)'
    )

    foreach ($match in $matches) {
        $block = $match.Groups['block'].Value
        $logicalName = $match.Groups['logicalName'].Value.Trim()
        $displayNameMatch = [regex]::Match($block, '(?m)^[^\S\r\n]{4}displayName:[^\S\r\n]*(?<value>.+?)\r?$')
        $primaryFieldMatch = [regex]::Match($block, '(?ms)^[^\S\r\n]{4}primaryName:[^\S\r\n]*\r?\n[^\S\r\n]{6}logicalName:[^\S\r\n]*(?<value>\S+)')
        $choiceSetMatches = [regex]::Matches($block, '(?m)^[^\S\r\n]+choiceSet:[^\S\r\n]*(?<value>\S+)[^\S\r\n]*\r?$')
        $lookupMatches = [regex]::Matches($block, '(?m)^[^\S\r\n]+relatedTable:[^\S\r\n]*(?<value>\S+)[^\S\r\n]*\r?$')

        $choiceDependencies = @(
            $choiceSetMatches | ForEach-Object { $_.Groups['value'].Value.Trim() } | Sort-Object -Unique
        )
        $lookupDependencies = @(
            $lookupMatches | ForEach-Object { $_.Groups['value'].Value.Trim() } | Sort-Object -Unique
        )

        $result.Add([ordered]@{
            logicalName = $logicalName
            displayName = $displayNameMatch.Groups['value'].Value.Trim()
            primaryField = $primaryFieldMatch.Groups['value'].Value.Trim()
            choiceDependencies = $choiceDependencies
            lookupDependencies = $lookupDependencies
        })
    }

    return $result
}

function Parse-YamlRelationships {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    $result = New-Object System.Collections.Generic.List[object]
    $matches = [regex]::Matches(
        $Text,
        '(?ms)^[^\S\r\n]{2}-[^\S\r\n]+name:[^\S\r\n]*(?<name>\S+)[^\S\r\n]*\r?\n(?<block>.*?)(?=^[^\S\r\n]{2}-[^\S\r\n]+name:|\z)'
    )

    foreach ($match in $matches) {
        $block = $match.Groups['block'].Value
        $result.Add([ordered]@{
            name = $match.Groups['name'].Value.Trim()
            fromTable = ([regex]::Match($block, '(?m)^[^\S\r\n]{4}fromTable:[^\S\r\n]*(?<value>\S+)[^\S\r\n]*\r?$').Groups['value'].Value).Trim()
            fromColumn = ([regex]::Match($block, '(?m)^[^\S\r\n]{4}fromColumn:[^\S\r\n]*(?<value>\S+)[^\S\r\n]*\r?$').Groups['value'].Value).Trim()
            toTable = ([regex]::Match($block, '(?m)^[^\S\r\n]{4}toTable:[^\S\r\n]*(?<value>\S+)[^\S\r\n]*\r?$').Groups['value'].Value).Trim()
            type = ([regex]::Match($block, '(?m)^[^\S\r\n]{4}type:[^\S\r\n]*(?<value>\S+)[^\S\r\n]*\r?$').Groups['value'].Value).Trim()
            required = ([regex]::Match($block, '(?m)^[^\S\r\n]{4}required:[^\S\r\n]*(?<value>\S+)[^\S\r\n]*\r?$').Groups['value'].Value).Trim()
            deleteBehavior = ([regex]::Match($block, '(?m)^[^\S\r\n]{4}deleteBehavior:[^\S\r\n]*(?<value>\S+)[^\S\r\n]*\r?$').Groups['value'].Value).Trim()
            group = ([regex]::Match($block, '(?m)^[^\S\r\n]{4}group:[^\S\r\n]*(?<value>\S+)[^\S\r\n]*\r?$').Groups['value'].Value).Trim()
        })
    }

    return $result
}

function Get-TopologicalTableOrder {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Tables
    )

    $indexByName = @{}
    for ($i = 0; $i -lt $Tables.Count; $i++) {
        $indexByName[$Tables[$i].logicalName] = $i
    }

    $incoming = @{}
    $outgoing = @{}

    foreach ($table in $Tables) {
        $deps = New-Object System.Collections.Generic.HashSet[string]
        foreach ($dep in $table.lookupDependencies) {
            if ($dep -eq $table.logicalName) { continue }
            if ($indexByName.ContainsKey($dep)) {
                $null = $deps.Add($dep)
            }
        }
        $incoming[$table.logicalName] = [System.Collections.Generic.HashSet[string]]$deps
        $outgoing[$table.logicalName] = New-Object System.Collections.Generic.List[string]
    }

    foreach ($table in $Tables) {
        foreach ($dep in $incoming[$table.logicalName]) {
            $outgoing[$dep].Add($table.logicalName)
        }
    }

    $queue = New-Object System.Collections.Generic.List[string]
    foreach ($table in $Tables) {
        if ($incoming[$table.logicalName].Count -eq 0) {
            $queue.Add($table.logicalName)
        }
    }

    $ordered = New-Object System.Collections.Generic.List[string]
    while ($queue.Count -gt 0) {
        $next = $queue | Sort-Object { $indexByName[$_] } | Select-Object -First 1
        $queue.Remove($next) | Out-Null
        $ordered.Add($next)

        foreach ($child in @($outgoing[$next])) {
            $null = $incoming[$child].Remove($next)
            if ($incoming[$child].Count -eq 0 -and -not $ordered.Contains($child) -and -not $queue.Contains($child)) {
                $queue.Add($child)
            }
        }
    }

    if ($ordered.Count -ne $Tables.Count) {
        $missing = @($Tables | Where-Object { -not $ordered.Contains($_.logicalName) } | ForEach-Object { $_.logicalName })
        throw "Unable to resolve table creation order. Remaining: $($missing -join ', ')"
    }

    return $ordered
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$tablesPath = Join-Path $SchemaRoot 'dataverse-tables.yaml'
$choicesPath = Join-Path $SchemaRoot 'choices.yaml'
$relationshipsPath = Join-Path $SchemaRoot 'relationships.yaml'

$tablesText = Get-FileText -Path $tablesPath
$choicesText = Get-FileText -Path $choicesPath
$relationshipsText = Get-FileText -Path $relationshipsPath

$tables = Parse-YamlTables -Text $tablesText
$relationships = Parse-YamlRelationships -Text $relationshipsText

$choiceNames = @(
    [regex]::Matches($choicesText, '(?m)^[^\S\r\n]{2}(?<name>[A-Za-z0-9_]+):[^\S\r\n]*\r?$') |
        ForEach-Object { $_.Groups['name'].Value.Trim() } |
        Sort-Object -Unique
)

$tableOrder = Get-TopologicalTableOrder -Tables $tables
$tableLookup = @{}
foreach ($table in $tables) {
    $tableLookup[$table.logicalName] = $table
}

$buildTables = foreach ($logicalName in $tableOrder) {
    $table = $tableLookup[$logicalName]
    [ordered]@{
        logicalName = $table.logicalName
        displayName = $table.displayName
        primaryField = $table.primaryField
        choiceDependencies = @($table.choiceDependencies | Sort-Object -Unique)
        lookupDependencies = @($table.lookupDependencies | Sort-Object -Unique)
    }
}

$relationshipPhases = [ordered]@{}
foreach ($group in @('mvp', 'future')) {
    $relationshipPhases[$group] = @(
        $relationships |
            Where-Object { $_.group -eq $group } |
            ForEach-Object {
                [ordered]@{
                    name = $_.name
                    fromTable = $_.fromTable
                    fromColumn = $_.fromColumn
                    toTable = $_.toTable
                    type = $_.type
                    required = [bool]::Parse($_.required)
                    deleteBehavior = $_.deleteBehavior
                }
            }
    )
}

$plan = [ordered]@{
    publisherPrefix = 'dft'
    schemaVersion = '0.1'
    sourceFiles = [ordered]@{
        tables = 'powerplatform/src/schema/dataverse-tables.yaml'
        choices = 'powerplatform/src/schema/choices.yaml'
        relationships = 'powerplatform/src/schema/relationships.yaml'
    }
    tableCreationOrder = @($buildTables)
    relationshipPhases = $relationshipPhases
    choiceSets = $choiceNames
}

$json = Get-OrderedJsonText -Value $plan

if ($ValidateOnly) {
    $existing = Get-FileText -Path $OutputPath
    $existingPlan = $existing | ConvertFrom-Json
    $generatedPlan = $json | ConvertFrom-Json
    $existingFingerprint = Get-PlanFingerprint -Plan $existingPlan | ConvertTo-Json -Depth 20
    $generatedFingerprint = Get-PlanFingerprint -Plan $generatedPlan | ConvertTo-Json -Depth 20
    $normalizedExistingFingerprint = Normalize-JsonText -Text $existingFingerprint
    $normalizedGeneratedFingerprint = Normalize-JsonText -Text $generatedFingerprint
    if ($normalizedExistingFingerprint -ne $normalizedGeneratedFingerprint) {
        $difference = Get-FirstDifference -Expected $existingFingerprint -Actual $generatedFingerprint
        throw "schema-build-plan.json does not match the exported plan. First fingerprint difference: $difference"
    }
    Write-Host "Schema build plan is current and valid."
    return
}

if (-not [string]::IsNullOrWhiteSpace((Split-Path -Parent $OutputPath))) {
    [System.IO.Directory]::CreateDirectory((Split-Path -Parent $OutputPath)) | Out-Null
}
[System.IO.File]::WriteAllText($OutputPath, $json)
Write-Host "Wrote schema build plan to $OutputPath"

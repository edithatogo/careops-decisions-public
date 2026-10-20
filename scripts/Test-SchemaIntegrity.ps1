[CmdletBinding()]
param(
    [switch]$PassThru
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$schemaRoot = Join-Path $repoRoot 'powerplatform/src/schema'
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

function Get-YamlKeyValues {
    param([string]$Path, [string]$Key)
    $text = Get-Content -LiteralPath $Path -Raw
    $results = [System.Collections.Generic.List[string]]::new()
    $pattern = "(?m)^\s{2}-\s+$([regex]::Escape($Key)):\s*(.+?)\s*$"
    $matches = [regex]::Matches($text, $pattern)
    foreach ($m in $matches) {
        $results.Add($m.Groups[1].Value.Trim())
    }
    return $results
}

function Get-TableNames {
    param([string]$Path)
    return Get-YamlKeyValues -Path $Path -Key 'logicalName'
}

function Get-ChoiceSetNames {
    param([string]$Path)
    $text = Get-Content -LiteralPath $Path -Raw
    $results = [System.Collections.Generic.List[string]]::new()
    $pattern = '(?m)^  ([\w_]+):\s*$'
    $matches = [regex]::Matches($text, $pattern)
    foreach ($m in $matches) {
        $results.Add($m.Groups[1].Value.Trim())
    }
    return $results
}

$tablesPath = Join-Path $schemaRoot 'dataverse-tables.yaml'
$choicesPath = Join-Path $schemaRoot 'choices.yaml'
$relationshipsPath = Join-Path $schemaRoot 'relationships.yaml'

if (-not (Test-Path -LiteralPath $tablesPath)) { Add-Failure "Missing schema file: dataverse-tables.yaml"; return }
if (-not (Test-Path -LiteralPath $choicesPath)) { Add-Failure "Missing schema file: choices.yaml"; return }
if (-not (Test-Path -LiteralPath $relationshipsPath)) { Add-Failure "Missing schema file: relationships.yaml"; return }

$tablesText = Get-Content -LiteralPath $tablesPath -Raw
$choicesText = Get-Content -LiteralPath $choicesPath -Raw
$relationshipsText = Get-Content -LiteralPath $relationshipsPath -Raw

$tableNames = Get-TableNames -Path $tablesPath
$choiceSetNames = Get-ChoiceSetNames -Path $choicesPath

$duplicateTables = $tableNames | Group-Object | Where-Object { $_.Count -gt 1 }
foreach ($dup in $duplicateTables) {
    Add-Failure "Duplicate table logicalName: '$($dup.Name)' appears $($dup.Count) times"
}

$duplicateChoices = $choiceSetNames | Group-Object | Where-Object { $_.Count -gt 1 }
foreach ($dup in $duplicateChoices) {
    Add-Failure "Duplicate choice set logicalName: '$($dup.Name)' appears $($dup.Count) times"
}

$choiceSetMatches = [regex]::Matches($tablesText, '(?m)^\s+choiceSet:\s*(.+?)\s*$')
foreach ($m in $choiceSetMatches) {
    $ref = $m.Groups[1].Value.Trim()
    if ($choiceSetNames -notcontains $ref) {
        Add-Failure "Table references choiceSet '$ref' which does not exist in choices.yaml"
    }
}

$lookupMatches = [regex]::Matches($tablesText, '(?m)^\s+relatedTable:\s*(.+?)\s*$')
foreach ($m in $lookupMatches) {
    $ref = $m.Groups[1].Value.Trim()
    if ($tableNames -notcontains $ref) {
        Add-Failure "Table references relatedTable '$ref' which does not exist in dataverse-tables.yaml"
    }
}

$tableMatches = [regex]::Matches($tablesText, '(?ms)logicalName:\s*(.+?)$.*?^columns:')
$tablesByName = @{}
$currentTable = $null
$currentTableCols = [System.Collections.Generic.List[string]]::new()
foreach ($line in $tablesText -split "`n") {
    if ($line -match '^\s{2}-\s+logicalName:\s*(.+?)\s*$') {
        if ($null -ne $currentTable) {
            $tablesByName[$currentTable] = @($currentTableCols)
        }
        $currentTable = $Matches[1].Trim()
        $currentTableCols = [System.Collections.Generic.List[string]]::new()
    }
    if ($null -ne $currentTable -and $line -match '^\s{8,10}-\s+logicalName:\s*(.+?)\s*$') {
        $currentTableCols.Add($Matches[1].Trim())
    }
}
if ($null -ne $currentTable) {
    $tablesByName[$currentTable] = @($currentTableCols)
}

foreach ($entry in $tablesByName.GetEnumerator()) {
    $dupes = $entry.Value | Group-Object | Where-Object { $_.Count -gt 1 }
    foreach ($dup in $dupes) {
        Add-Failure "Duplicate column logicalName '$($dup.Name)' in table '$($entry.Key)' appears $($dup.Count) times"
    }
}

$currentChoiceSet = $null
$currentValues = [System.Collections.Generic.List[string]]::new()
foreach ($line in $choicesText -split "`n") {
    if ($line -match '^  (\w[\w_]*):$' -and $null -eq ($line -match '^\s{4,}')) {
        if ($null -ne $currentChoiceSet) {
            $dupes = $currentValues | Group-Object | Where-Object { $_.Count -gt 1 }
            foreach ($dup in $dupes) {
                Add-Failure "Duplicate choice value '$($dup.Name)' in choice set '$currentChoiceSet' appears $($dup.Count) times"
            }
        }
        $currentChoiceSet = $Matches[1]
        $currentValues = [System.Collections.Generic.List[string]]::new()
    }
    if ($null -ne $currentChoiceSet -and $line -match '^\s{6}code:\s*(.+?)\s*$') {
        $currentValues.Add($Matches[1].Trim())
    }
}
if ($null -ne $currentChoiceSet) {
    $dupes = $currentValues | Group-Object | Where-Object { $_.Count -gt 1 }
    foreach ($dup in $dupes) {
        Add-Failure "Duplicate choice value '$($dup.Name)' in choice set '$currentChoiceSet' appears $($dup.Count) times"
    }
}

$relNames = [System.Collections.Generic.List[string]]::new()
$relFromTables = [System.Collections.Generic.List[string]]::new()
$relToTables = [System.Collections.Generic.List[string]]::new()
$relLines = $relationshipsText -split "`n"
$currentRel = $null
$currentFrom = $null
$currentTo = $null
foreach ($line in $relLines) {
    if ($line -match '^\s{2}-\s+name:\s*(.+?)\s*$') {
        if ($null -ne $currentRel) {
            $relNames.Add($currentRel)
            $relFromTables.Add($currentFrom)
            $relToTables.Add($currentTo)
        }
        $currentRel = $Matches[1].Trim()
        $currentFrom = $null
        $currentTo = $null
    }
    if ($null -ne $currentRel -and $line -match '^\s{4}fromTable:\s*(.+?)\s*$') {
        $currentFrom = $Matches[1].Trim()
    }
    if ($null -ne $currentRel -and $line -match '^\s{4}toTable:\s*(.+?)\s*$') {
        $currentTo = $Matches[1].Trim()
    }
}
if ($null -ne $currentRel) {
    $relNames.Add($currentRel)
    $relFromTables.Add($currentFrom)
    $relToTables.Add($currentTo)
}

$duplicateRels = $relNames | Group-Object | Where-Object { $_.Count -gt 1 }
foreach ($dup in $duplicateRels) {
    Add-Failure "Duplicate relationship name: '$($dup.Name)' appears $($dup.Count) times"
}

for ($i = 0; $i -lt $relFromTables.Count; $i++) {
    $ft = $relFromTables[$i]
    $tt = $relToTables[$i]
    $rn = $relNames[$i]
    if ($tableNames -notcontains $ft) {
        Add-Failure "Relationship '$rn' references fromTable '$ft' which does not exist in dataverse-tables.yaml"
    }
    if ($tableNames -notcontains $tt) {
        Add-Failure "Relationship '$rn' references toTable '$tt' which does not exist in dataverse-tables.yaml"
    }
}

if ($failures.Count -eq 0) {
    Write-Host "Schema integrity validation passed. ($($failures.Count) failures, $($warnings.Count) warnings)"
} else {
    foreach ($f in $failures) { Write-Error $f }
    foreach ($w in $warnings) { Write-Warning $w }
    if (-not $PassThru) { exit 1 }
}

if ($PassThru) {
    [pscustomobject]@{
        Passed    = ($failures.Count -eq 0)
        Failures  = @($failures)
        Warnings  = @($warnings)
        Summary   = "Schema integrity validation with $($failures.Count) failures and $($warnings.Count) warnings."
    }
}

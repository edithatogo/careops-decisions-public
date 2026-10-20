[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$PlanPath,
    [switch]$PlanOnly,
    [switch]$Execute,
    [switch]$ConfirmExecute
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Resolve-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $repoRoot = Split-Path -Parent $PSScriptRoot
    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $RelativePath))
}

function Resolve-PlanInputPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return Resolve-RepoPath -RelativePath $Path
}

function Read-RequiredText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Missing file: $Path"
    }

    return [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $Path))
}

function Get-CommandPrefix {
    $pacx = Get-Command pacx -ErrorAction SilentlyContinue
    if ($pacx) {
        return 'pacx'
    }

    $pac = Get-Command pac -ErrorAction SilentlyContinue
    if ($pac) {
        return 'pac'
    }

    return 'pacx'
}

function Quote-CommandValue {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Value
    )

    $text = [string]$Value
    if ([string]::IsNullOrWhiteSpace($text)) {
        return '""'
    }

    $escaped = $text.Replace('"', '\"')
    return '"' + $escaped + '"'
}

function Parse-TableManifest {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    $result = @()
    $matches = [regex]::Matches(
        $Text,
        '(?ms)^\s{2}-\slogicalName:\s*(?<logicalName>\S+)\s*\r?\n(?<block>.*?)(?=^\s{2}-\slogicalName:|\z)'
    )

    foreach ($match in $matches) {
        $block = $match.Groups['block'].Value
        $logicalName = $match.Groups['logicalName'].Value.Trim()

        $displayName = ([regex]::Match($block, '(?m)^\s{4}displayName:\s*(?<value>.+)$').Groups['value'].Value).Trim()
        $primaryNameBlock = [regex]::Match(
            $block,
            '(?ms)^\s{4}primaryName:\s*\r?\n(?<primaryBlock>.*?)(?=^\s{4}(?:alternateKeys:|columns:|mvpNotes:|futureNotes:|\S)|\z)'
        )
        $primaryBlock = $primaryNameBlock.Groups['primaryBlock'].Value

        $primaryField = ([regex]::Match($primaryBlock, '(?m)^\s{6}logicalName:\s*(?<value>\S+)$').Groups['value'].Value).Trim()
        $primaryLabel = ([regex]::Match($primaryBlock, '(?m)^\s{6}label:\s*(?<value>.+)$').Groups['value'].Value).Trim()
        $primaryType = ([regex]::Match($primaryBlock, '(?m)^\s{6}type:\s*(?<value>\S+)$').Groups['value'].Value).Trim()
        $primaryRequired = ([regex]::Match($primaryBlock, '(?m)^\s{6}required:\s*(?<value>\S+)$').Groups['value'].Value).Trim()
        $primaryMaxLength = ([regex]::Match($primaryBlock, '(?m)^\s{6}maxLength:\s*(?<value>\S+)$').Groups['value'].Value).Trim()

        $columnsSectionMatch = [regex]::Match(
            $block,
            '(?ms)^\s{4}columns:\s*\r?\n(?<columnsBlock>.*?)(?=^\s{4}(?:alternateKeys:|mvpNotes:|futureNotes:|\S)|\z)'
        )
        $columns = @()

        if ($columnsSectionMatch.Success) {
            $columnMatches = [regex]::Matches(
                $columnsSectionMatch.Groups['columnsBlock'].Value,
                '(?ms)^\s{6}-\slogicalName:\s*(?<logicalName>\S+)\s*\r?\n(?<columnBlock>.*?)(?=^\s{6}-\slogicalName:|\z)'
            )

            foreach ($columnMatch in $columnMatches) {
                $columnBlock = $columnMatch.Groups['columnBlock'].Value
                $logicalColumnName = $columnMatch.Groups['logicalName'].Value.Trim()
                $columns += [pscustomobject]@{
                    logicalName = $logicalColumnName
                    label = ([regex]::Match($columnBlock, '(?m)^\s{8}label:\s*(?<value>.+)$').Groups['value'].Value).Trim()
                    type = ([regex]::Match($columnBlock, '(?m)^\s{8}type:\s*(?<value>\S+)$').Groups['value'].Value).Trim()
                    choiceSet = ([regex]::Match($columnBlock, '(?m)^\s{8}choiceSet:\s*(?<value>\S+)$').Groups['value'].Value).Trim()
                    relatedTable = ([regex]::Match($columnBlock, '(?m)^\s{8}relatedTable:\s*(?<value>\S+)$').Groups['value'].Value).Trim()
                    required = ([regex]::Match($columnBlock, '(?m)^\s{8}required:\s*(?<value>\S+)$').Groups['value'].Value).Trim()
                    unique = ([regex]::Match($columnBlock, '(?m)^\s{8}unique:\s*(?<value>\S+)$').Groups['value'].Value).Trim()
                    maxLength = ([regex]::Match($columnBlock, '(?m)^\s{8}maxLength:\s*(?<value>\S+)$').Groups['value'].Value).Trim()
                }
            }
        }

        $alternateKeysSectionMatch = [regex]::Match(
            $block,
            '(?ms)^\s{4}alternateKeys:\s*\r?\n(?<alternateKeysBlock>.*?)(?=^\s{4}(?:columns:|mvpNotes:|futureNotes:|\S)|\z)'
        )
        $alternateKeys = @()

        if ($alternateKeysSectionMatch.Success) {
            $keyMatches = [regex]::Matches(
                $alternateKeysSectionMatch.Groups['alternateKeysBlock'].Value,
                '(?ms)^\s{6}-\sname:\s*(?<name>\S+)\s*\r?\n(?<keyBlock>.*?)(?=^\s{6}-\sname:|\z)'
            )

            foreach ($keyMatch in $keyMatches) {
                $keyBlock = $keyMatch.Groups['keyBlock'].Value
                $columnsValue = ([regex]::Match($keyBlock, '(?m)^\s{8}columns:\s*\[(?<value>[^\]]*)\]').Groups['value'].Value).Trim()
                $keyColumns = @()
                if ($columnsValue) {
                    $keyColumns = @(
                        $columnsValue.Split(',') |
                            ForEach-Object { $_.Trim() } |
                            Where-Object { $_ }
                    )
                }

                $alternateKeys += [pscustomobject]@{
                    name = $keyMatch.Groups['name'].Value.Trim()
                    columns = $keyColumns
                }
            }
        }

        $result += [pscustomobject]@{
            logicalName = $logicalName
            displayName = $displayName
            primaryField = $primaryField
            primaryLabel = $primaryLabel
            primaryType = $primaryType
            primaryRequired = $primaryRequired
            primaryMaxLength = $primaryMaxLength
            columns = $columns
            alternateKeys = $alternateKeys
        }
    }

    return $result
}

function Get-ChoiceOrder {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Plan
    )

    $choiceOrder = New-Object System.Collections.Generic.List[string]
    $seen = @{}

    foreach ($table in @($Plan.tableCreationOrder)) {
        foreach ($choice in @($table.choiceDependencies)) {
            if ([string]::IsNullOrWhiteSpace([string]$choice)) {
                continue
            }

            $choiceName = [string]$choice
            if (-not $seen.ContainsKey($choiceName)) {
                $seen[$choiceName] = $true
                $choiceOrder.Add($choiceName)
            }
        }
    }

    foreach ($choice in @($Plan.choiceSets)) {
        if ([string]::IsNullOrWhiteSpace([string]$choice)) {
            continue
        }

        $choiceName = [string]$choice
        if (-not $seen.ContainsKey($choiceName)) {
            $seen[$choiceName] = $true
            $choiceOrder.Add($choiceName)
        }
    }

    return @($choiceOrder)
}

function Get-HumanizedChoiceLabel {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ChoiceName
    )

    return $ChoiceName
}

function New-ChoiceCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Prefix,
        [Parameter(Mandatory = $true)]
        [string]$ChoiceName
    )

    $label = Get-HumanizedChoiceLabel -ChoiceName $ChoiceName
    return @(
        $Prefix,
        '<choice-set-create>',
        '--name', (Quote-CommandValue $ChoiceName),
        '--label', (Quote-CommandValue $label)
    ) -join ' '
}

function New-TableCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Prefix,
        [Parameter(Mandatory = $true)]
        [object]$Table
    )

    return @(
        $Prefix,
        '<table-create>',
        '--logical-name', (Quote-CommandValue $Table.logicalName),
        '--display-name', (Quote-CommandValue $Table.displayName),
        '--primary-field', (Quote-CommandValue $Table.primaryField)
    ) -join ' '
}

function New-PrimaryFieldCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Prefix,
        [Parameter(Mandatory = $true)]
        [object]$Table
    )

    $parts = @(
        $Prefix,
        '<primary-field-create>',
        '--table', (Quote-CommandValue $Table.logicalName),
        '--logical-name', (Quote-CommandValue $Table.primaryField)
    )

    if (-not [string]::IsNullOrWhiteSpace([string]$Table.primaryLabel)) {
        $parts += @('--label', (Quote-CommandValue $Table.primaryLabel))
    }
    if (-not [string]::IsNullOrWhiteSpace([string]$Table.primaryType)) {
        $parts += @('--type', (Quote-CommandValue $Table.primaryType))
    }
    if (-not [string]::IsNullOrWhiteSpace([string]$Table.primaryRequired)) {
        $parts += @('--required', (Quote-CommandValue $Table.primaryRequired))
    }
    if (-not [string]::IsNullOrWhiteSpace([string]$Table.primaryMaxLength)) {
        $parts += @('--max-length', (Quote-CommandValue $Table.primaryMaxLength))
    }

    return $parts -join ' '
}

function New-ColumnCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Prefix,
        [Parameter(Mandatory = $true)]
        [object]$Table,
        [Parameter(Mandatory = $true)]
        [object]$Column
    )

    $parts = @(
        $Prefix,
        '<column-create>',
        '--table', (Quote-CommandValue $Table.logicalName),
        '--logical-name', (Quote-CommandValue $Column.logicalName)
    )

    if (-not [string]::IsNullOrWhiteSpace([string]$Column.label)) {
        $parts += @('--label', (Quote-CommandValue $Column.label))
    }
    if (-not [string]::IsNullOrWhiteSpace([string]$Column.type)) {
        $parts += @('--type', (Quote-CommandValue $Column.type))
    }
    if (-not [string]::IsNullOrWhiteSpace([string]$Column.choiceSet)) {
        $parts += @('--choice-set', (Quote-CommandValue $Column.choiceSet))
    }
    if (-not [string]::IsNullOrWhiteSpace([string]$Column.relatedTable)) {
        $parts += @('--related-table', (Quote-CommandValue $Column.relatedTable))
    }
    if (-not [string]::IsNullOrWhiteSpace([string]$Column.required)) {
        $parts += @('--required', (Quote-CommandValue $Column.required))
    }
    if (-not [string]::IsNullOrWhiteSpace([string]$Column.unique)) {
        $parts += @('--unique', (Quote-CommandValue $Column.unique))
    }
    if (-not [string]::IsNullOrWhiteSpace([string]$Column.maxLength)) {
        $parts += @('--max-length', (Quote-CommandValue $Column.maxLength))
    }

    return $parts -join ' '
}

function New-KeyCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Prefix,
        [Parameter(Mandatory = $true)]
        [object]$Table,
        [Parameter(Mandatory = $true)]
        [object]$Key
    )

    return @(
        $Prefix,
        '<alternate-key-create>',
        '--table', (Quote-CommandValue $Table.logicalName),
        '--name', (Quote-CommandValue $Key.name),
        '--columns', (Quote-CommandValue (($Key.columns -join ',')))
    ) -join ' '
}

function New-RelationshipCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Prefix,
        [Parameter(Mandatory = $true)]
        [object]$Relationship
    )

    $parts = @(
        $Prefix,
        '<relationship-create>',
        '--name', (Quote-CommandValue $Relationship.name),
        '--from-table', (Quote-CommandValue $Relationship.fromTable),
        '--from-column', (Quote-CommandValue $Relationship.fromColumn),
        '--to-table', (Quote-CommandValue $Relationship.toTable)
    )

    if (-not [string]::IsNullOrWhiteSpace([string]$Relationship.type)) {
        $parts += @('--type', (Quote-CommandValue $Relationship.type))
    }
    if ($null -ne $Relationship.required) {
        $parts += @('--required', (Quote-CommandValue $Relationship.required))
    }
    if (-not [string]::IsNullOrWhiteSpace([string]$Relationship.deleteBehavior)) {
        $parts += @('--delete-behavior', (Quote-CommandValue $Relationship.deleteBehavior))
    }

    return $parts -join ' '
}

function Write-Step {
    param(
        [Parameter(Mandatory = $true)]
        [int]$Number,
        [Parameter(Mandatory = $true)]
        [string]$Phase,
        [Parameter(Mandatory = $true)]
        [string]$Subject,
        [Parameter(Mandatory = $true)]
        [string]$Command,
        [string]$Details
    )

    if ([string]::IsNullOrWhiteSpace($Details)) {
        Write-Host ("{0,3}. [{1}] {2}" -f $Number, $Phase, $Subject)
        Write-Host ("     {0}" -f $Command)
    }
    else {
        Write-Host ("{0,3}. [{1}] {2}" -f $Number, $Phase, $Subject)
        Write-Host ("     {0}" -f $Details)
        Write-Host ("     {0}" -f $Command)
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$defaultPlanPath = Join-Path $repoRoot 'powerplatform/src/schema/schema-build-plan.json'
if ([string]::IsNullOrWhiteSpace($PlanPath)) {
    $PlanPath = $defaultPlanPath
}
$resolvedPlanPath = Resolve-PlanInputPath -Path $PlanPath

if (-not (Test-Path -LiteralPath $resolvedPlanPath)) {
    throw "Missing schema build plan: $resolvedPlanPath"
}

if ($Execute -and $PlanOnly) {
    throw "Use either -Execute or -PlanOnly, not both."
}

if ($Execute -and -not $ConfirmExecute) {
    throw "Pass -ConfirmExecute to acknowledge execution-oriented output."
}

$schemaRoot = Resolve-RepoPath -RelativePath 'powerplatform/src/schema'
$tablesManifestPath = Join-Path $schemaRoot 'dataverse-tables.yaml'

$plan = Read-RequiredText -Path $resolvedPlanPath | ConvertFrom-Json
$tableManifest = @{}

if (Test-Path -LiteralPath $tablesManifestPath) {
    foreach ($table in @(Parse-TableManifest -Text (Read-RequiredText -Path $tablesManifestPath))) {
        $tableManifest[$table.logicalName] = $table
    }
}

$commandPrefix = Get-CommandPrefix
$mode = if ($Execute) { 'Execute' } elseif ($PlanOnly) { 'PlanOnly' } else { 'Plan' }
$choiceOrder = Get-ChoiceOrder -Plan $plan
$orderedTables = @($plan.tableCreationOrder)

Write-Host "Dataverse schema build"
Write-Host ("Repository root: {0}" -f $repoRoot)
Write-Host ("Plan file: {0}" -f $resolvedPlanPath)
Write-Host ("Command prefix: {0}" -f $commandPrefix)
Write-Host ("Mode: {0}" -f $mode)
Write-Host ""

$stepNumber = 1

Write-Host "Choices"
foreach ($choiceName in $choiceOrder) {
    $usageTables = @(
        $orderedTables |
            Where-Object { @($_.choiceDependencies) -contains $choiceName } |
            ForEach-Object { $_.logicalName }
    )

    $details = if ($usageTables.Count -gt 0) {
        "Used by: $($usageTables -join ', ')"
    }
    else {
        'Referenced by the schema plan.'
    }

    Write-Step -Number $stepNumber -Phase 'choices' -Subject $choiceName -Details $details -Command (New-ChoiceCommand -Prefix $commandPrefix -ChoiceName $choiceName)
    $stepNumber++
}

Write-Host ""
Write-Host "Tables"
foreach ($table in $orderedTables) {
    Write-Step -Number $stepNumber -Phase 'tables' -Subject $table.logicalName -Details $table.displayName -Command (New-TableCommand -Prefix $commandPrefix -Table $table)
    $stepNumber++
}

Write-Host ""
Write-Host "Primary fields"
foreach ($table in $orderedTables) {
    $tableDetails = if ($tableManifest.ContainsKey($table.logicalName)) { $tableManifest[$table.logicalName] } else { $null }
    $primaryTemplate = if ($tableDetails) { $tableDetails } else { $table }
    Write-Step -Number $stepNumber -Phase 'primary-fields' -Subject ("{0}.{1}" -f $table.logicalName, $table.primaryField) -Details $table.displayName -Command (New-PrimaryFieldCommand -Prefix $commandPrefix -Table $primaryTemplate)
    $stepNumber++
}

Write-Host ""
Write-Host "Columns"
foreach ($table in $orderedTables) {
    $tableDetails = $null
    if ($tableManifest.ContainsKey($table.logicalName)) {
        $tableDetails = $tableManifest[$table.logicalName]
    }

    if ($null -eq $tableDetails -or @($tableDetails.columns).Count -eq 0) {
        Write-Step -Number $stepNumber -Phase 'columns' -Subject $table.logicalName -Details 'Column details were not available in dataverse-tables.yaml.' -Command ("$commandPrefix <column-create> --table " + (Quote-CommandValue $table.logicalName))
        $stepNumber++
        continue
    }

    foreach ($column in @($tableDetails.columns)) {
        Write-Step -Number $stepNumber -Phase 'columns' -Subject ("{0}.{1}" -f $table.logicalName, $column.logicalName) -Details $column.label -Command (New-ColumnCommand -Prefix $commandPrefix -Table $tableDetails -Column $column)
        $stepNumber++
    }
}

Write-Host ""
Write-Host "Keys"
foreach ($table in $orderedTables) {
    $tableDetails = $null
    if ($tableManifest.ContainsKey($table.logicalName)) {
        $tableDetails = $tableManifest[$table.logicalName]
    }

    if ($null -eq $tableDetails -or @($tableDetails.alternateKeys).Count -eq 0) {
        Write-Step -Number $stepNumber -Phase 'keys' -Subject $table.logicalName -Details 'No alternate keys are declared for this table.' -Command ("$commandPrefix <alternate-key-create> --table " + (Quote-CommandValue $table.logicalName))
        $stepNumber++
        continue
    }

    foreach ($key in @($tableDetails.alternateKeys)) {
        $keyColumns = @($key.columns) -join ', '
        Write-Step -Number $stepNumber -Phase 'keys' -Subject ("{0}.{1}" -f $table.logicalName, $key.name) -Details ("Columns: {0}" -f $keyColumns) -Command (New-KeyCommand -Prefix $commandPrefix -Table $tableDetails -Key $key)
        $stepNumber++
    }
}

Write-Host ""
Write-Host "Relationships"
foreach ($phaseName in @('mvp', 'future')) {
    Write-Host ("[{0}]" -f $phaseName)
    foreach ($relationship in @($plan.relationshipPhases.$phaseName)) {
        Write-Step -Number $stepNumber -Phase ("relationships/{0}" -f $phaseName) -Subject $relationship.name -Details ("{0}.{1} -> {2} ({3}, required={4}, delete={5})" -f $relationship.fromTable, $relationship.fromColumn, $relationship.toTable, $relationship.type, $relationship.required, $relationship.deleteBehavior) -Command (New-RelationshipCommand -Prefix $commandPrefix -Relationship $relationship)
        $stepNumber++
    }
    Write-Host ""
}

Write-Host ("Total steps: {0}" -f ($stepNumber - 1))
if ($Execute) {
    Write-Host "Execution-oriented mode requested. The script still emits command templates only so you can confirm the CLI mapping before any live Dataverse work."
}

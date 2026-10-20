param(
    [string]$OutputRoot = "powerplatform/generated/dataverse",
    [switch]$ValidateOnly
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$schemaRoot = Join-Path $repoRoot 'powerplatform/src/schema'
$planPath = Join-Path $schemaRoot 'schema-build-plan.json'
$tablesPath = Join-Path $schemaRoot 'dataverse-tables.yaml'
$choicesPath = Join-Path $schemaRoot 'choices.yaml'
$resolvedOutputRoot = if ([System.IO.Path]::IsPathRooted($OutputRoot)) {
    [System.IO.Path]::GetFullPath($OutputRoot)
} else {
    [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputRoot))
}

function Read-Text {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Missing required file: $Path"
    }
    return [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $Path))
}

function Normalize-GeneratedArtifact {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Text
    )

    if ([System.IO.Path]::GetExtension($Path) -eq '.json') {
        return (($Text | ConvertFrom-Json) | ConvertTo-Json -Depth 100 -Compress)
    }

    return (($Text.Trim() -replace "`r`n", "`n") -replace "`r", "`n")
}

function Quote-PowerShellValue {
    param([Parameter(Mandatory = $true)][object]$Value)
    $text = [string]$Value
    return "'" + $text.Replace("'", "''") + "'"
}

function Get-PublisherSchemaName {
    param([Parameter(Mandatory = $true)][string]$LogicalName)
    if ($LogicalName.StartsWith('dft_', [System.StringComparison]::OrdinalIgnoreCase)) {
        return $LogicalName
    }
    return "dft_$LogicalName"
}

function Get-DataverseLogicalName {
    param([Parameter(Mandatory = $true)][string]$SchemaName)
    return $SchemaName.ToLowerInvariant()
}

function Convert-ToRequiredLevel {
    param([object]$Required)
    if ([string]::IsNullOrWhiteSpace([string]$Required)) {
        return 'None'
    }
    if ([string]$Required -eq 'true') {
        return 'ApplicationRequired'
    }
    return 'None'
}

function Convert-ToColumnType {
    param([string]$Type)
    switch ($Type) {
        'text' { 'String'; break }
        'multilineText' { 'Memo'; break }
        'wholeNumber' { 'Integer'; break }
        'decimal' { 'Decimal'; break }
        'dateOnly' { 'DateTime'; break }
        'dateTime' { 'DateTime'; break }
        'yesNo' { 'Boolean'; break }
        'choice' { 'Picklist'; break }
        default { 'String' }
    }
}

function Resolve-KeyColumns {
    param(
        [Parameter(Mandatory = $true)][object]$Table,
        [Parameter(Mandatory = $true)][object[]]$Columns
    )

    $columnLookup = @{}
    foreach ($column in @($Table.columns)) {
        $columnLookup[$column.logicalName] = $column
    }

    return @($Columns | ForEach-Object {
        $columnName = [string]$_
        if ($columnLookup.ContainsKey($columnName) -and $columnLookup[$columnName].type -eq 'choice') {
            "$($columnName)code"
        } else {
            $columnName
        }
    })
}

function Get-ChildBlockMatches {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][int]$Indent,
        [Parameter(Mandatory = $true)][string]$Key
    )

    $space = [regex]::Escape((' ' * $Indent))
    return [regex]::Matches(
        $Text,
        "(?ms)^$space-[^\S\r\n]+$Key`:[^\S\r\n]*(?<name>\S+)[^\S\r\n]*\r?\n(?<block>.*?)(?=^$space-[^\S\r\n]+$Key`:|\z)"
    )
}

function Get-ScalarValue {
    param(
        [Parameter(Mandatory = $true)][string]$Block,
        [Parameter(Mandatory = $true)][int]$Indent,
        [Parameter(Mandatory = $true)][string]$Key
    )

    $space = [regex]::Escape((' ' * $Indent))
    $match = [regex]::Match($Block, "(?m)^$space$Key`:[^\S\r\n]*(?<value>.*?)[^\S\r\n]*\r?$")
    if (-not $match.Success) {
        return ''
    }
    return $match.Groups['value'].Value.Trim()
}

function Parse-TableManifest {
    param([Parameter(Mandatory = $true)][string]$Text)

    $tables = @()
    foreach ($tableMatch in Get-ChildBlockMatches -Text $Text -Indent 2 -Key 'logicalName') {
        $tableBlock = $tableMatch.Groups['block'].Value
        $primaryBlockMatch = [regex]::Match(
            $tableBlock,
            '(?ms)^[^\S\r\n]{4}primaryName:[^\S\r\n]*\r?\n(?<block>.*?)(?=^[^\S\r\n]{4}(?:alternateKeys:|columns:|mvpNotes:|futureNotes:|\S)|\z)'
        )
        $primaryBlock = if ($primaryBlockMatch.Success) { $primaryBlockMatch.Groups['block'].Value } else { '' }

        $columns = @()
        $columnsBlockMatch = [regex]::Match(
            $tableBlock,
            '(?ms)^[^\S\r\n]{4}columns:[^\S\r\n]*\r?\n(?<block>.*?)(?=^[^\S\r\n]{4}(?:alternateKeys:|mvpNotes:|futureNotes:|\S)|\z)'
        )
        if ($columnsBlockMatch.Success) {
            foreach ($columnMatch in Get-ChildBlockMatches -Text $columnsBlockMatch.Groups['block'].Value -Indent 6 -Key 'logicalName') {
                $columnBlock = $columnMatch.Groups['block'].Value
                $columns += [ordered]@{
                    logicalName = $columnMatch.Groups['name'].Value.Trim()
                    label = Get-ScalarValue -Block $columnBlock -Indent 8 -Key 'label'
                    type = Get-ScalarValue -Block $columnBlock -Indent 8 -Key 'type'
                    choiceSet = Get-ScalarValue -Block $columnBlock -Indent 8 -Key 'choiceSet'
                    relatedTable = Get-ScalarValue -Block $columnBlock -Indent 8 -Key 'relatedTable'
                    required = Get-ScalarValue -Block $columnBlock -Indent 8 -Key 'required'
                    maxLength = Get-ScalarValue -Block $columnBlock -Indent 8 -Key 'maxLength'
                    unique = Get-ScalarValue -Block $columnBlock -Indent 8 -Key 'unique'
                }
            }
        }

        $alternateKeys = @()
        $keysBlockMatch = [regex]::Match(
            $tableBlock,
            '(?ms)^[^\S\r\n]{4}alternateKeys:[^\S\r\n]*\r?\n(?<block>.*?)(?=^[^\S\r\n]{4}(?:columns:|mvpNotes:|futureNotes:|\S)|\z)'
        )
        if ($keysBlockMatch.Success) {
            foreach ($keyMatch in Get-ChildBlockMatches -Text $keysBlockMatch.Groups['block'].Value -Indent 6 -Key 'name') {
                $columnsValue = Get-ScalarValue -Block $keyMatch.Groups['block'].Value -Indent 8 -Key 'columns'
                $keyColumns = @()
                if ($columnsValue -match '^\[(?<value>.*)\]$') {
                    $keyColumns = @(
                        $Matches['value'].Split(',') |
                            ForEach-Object { $_.Trim() } |
                            Where-Object { $_ }
                    )
                }
                $alternateKeys += [ordered]@{
                    name = $keyMatch.Groups['name'].Value.Trim()
                    columns = $keyColumns
                }
            }
        }

        $tables += [ordered]@{
            logicalName = $tableMatch.Groups['name'].Value.Trim()
            displayName = Get-ScalarValue -Block $tableBlock -Indent 4 -Key 'displayName'
            description = Get-ScalarValue -Block $tableBlock -Indent 4 -Key 'description'
            primaryName = [ordered]@{
                logicalName = Get-ScalarValue -Block $primaryBlock -Indent 6 -Key 'logicalName'
                label = Get-ScalarValue -Block $primaryBlock -Indent 6 -Key 'label'
                type = Get-ScalarValue -Block $primaryBlock -Indent 6 -Key 'type'
                required = Get-ScalarValue -Block $primaryBlock -Indent 6 -Key 'required'
                maxLength = Get-ScalarValue -Block $primaryBlock -Indent 6 -Key 'maxLength'
            }
            columns = $columns
            alternateKeys = $alternateKeys
        }
    }

    return $tables
}

function Parse-Choices {
    param([Parameter(Mandatory = $true)][string]$Text)

    $choices = @()
    $matches = [regex]::Matches(
        $Text,
        '(?ms)^[^\S\r\n]{2}(?<name>[A-Za-z0-9_]+):[^\S\r\n]*\r?\n(?<block>.*?)(?=^[^\S\r\n]{2}[A-Za-z0-9_]+:|\z)'
    )

    foreach ($match in $matches) {
        $block = $match.Groups['block'].Value
        $values = @()
        foreach ($valueMatch in [regex]::Matches($block, '(?ms)^[^\S\r\n]{6}-[^\S\r\n]+code:[^\S\r\n]*(?<code>\S+)[^\S\r\n]*\r?\n(?<valueBlock>.*?)(?=^[^\S\r\n]{6}-[^\S\r\n]+code:|\z)')) {
            $valueBlock = $valueMatch.Groups['valueBlock'].Value
            $values += [ordered]@{
                code = $valueMatch.Groups['code'].Value.Trim()
                label = Get-ScalarValue -Block $valueBlock -Indent 8 -Key 'label'
            }
        }

        $choices += [ordered]@{
            logicalName = $match.Groups['name'].Value.Trim()
            schemaName = Get-PublisherSchemaName -LogicalName $match.Groups['name'].Value.Trim()
            description = Get-ScalarValue -Block $block -Indent 4 -Key 'description'
            values = $values
        }
    }

    return $choices
}

function New-CommandStep {
    param(
        [int]$Order,
        [string]$Phase,
        [string]$Name,
        [string]$Command,
        [string]$Notes = ''
    )

    return [ordered]@{
        order = $Order
        phase = $Phase
        name = $Name
        command = $Command
        notes = $Notes
    }
}

$plan = Read-Text -Path $planPath | ConvertFrom-Json
$tables = Parse-TableManifest -Text (Read-Text -Path $tablesPath)
$choices = Parse-Choices -Text (Read-Text -Path $choicesPath)
$tableLookup = @{}
foreach ($table in $tables) {
    $tableLookup[$table.logicalName] = $table
}
$choiceLookup = @{}
foreach ($choice in $choices) {
    $choiceLookup[$choice.logicalName] = $choice
}

$steps = New-Object System.Collections.Generic.List[object]
$order = 1
$solutionName = 'DecisionsWorkspace'

foreach ($choiceName in @($plan.choiceSets | Sort-Object)) {
    if (-not $choiceLookup.ContainsKey($choiceName)) {
        throw "Schema build plan references an undefined choice set: $choiceName"
    }
    $choice = $choiceLookup[$choiceName]
    $options = (@($choice.values) | ForEach-Object { $_.label }) -join ','
    $command = @(
        'pacx optionset create',
        '--name', (Quote-PowerShellValue $choice.logicalName),
        '--schemaName', (Quote-PowerShellValue $choice.schemaName),
        '--options', (Quote-PowerShellValue $options),
        '--solution', (Quote-PowerShellValue $solutionName)
    ) -join ' '
    $steps.Add((New-CommandStep -Order $order -Phase 'choices' -Name $choice.logicalName -Command $command -Notes ("{0} options" -f @($choice.values).Count)))
    $order++
}

foreach ($orderedTable in @($plan.tableCreationOrder)) {
    $table = if ($tableLookup.ContainsKey($orderedTable.logicalName)) { $tableLookup[$orderedTable.logicalName] } else { $null }
    if ($null -eq $table) {
        continue
    }

    $command = @(
        'pacx table create',
        '--name', (Quote-PowerShellValue $table.displayName),
        '--schemaName', (Quote-PowerShellValue $table.logicalName),
        '--primaryAttributeName', (Quote-PowerShellValue $table.primaryName.label),
        '--primaryAttributeSchemaName', (Quote-PowerShellValue $table.primaryName.logicalName),
        '--primaryAttributeRequiredLevel', (Quote-PowerShellValue (Convert-ToRequiredLevel -Required $table.primaryName.required)),
        '--primaryAttributeMaxLength', (Quote-PowerShellValue ($(if ($table.primaryName.maxLength) { $table.primaryName.maxLength } else { '100' }))),
        '--description', (Quote-PowerShellValue $table.description),
        '--solution', (Quote-PowerShellValue $solutionName)
    ) -join ' '
    $steps.Add((New-CommandStep -Order $order -Phase 'tables' -Name $table.logicalName -Command $command))
    $order++
}

foreach ($orderedTable in @($plan.tableCreationOrder)) {
    if (-not $tableLookup.ContainsKey($orderedTable.logicalName)) {
        continue
    }
    $table = $tableLookup[$orderedTable.logicalName]
    foreach ($column in @($table.columns)) {
        if ($column.type -eq 'lookup') {
            continue
        }

        $columnType = Convert-ToColumnType -Type $column.type
        $command = @(
            'pacx column create',
            '--table', (Quote-PowerShellValue (Get-DataverseLogicalName -SchemaName $table.logicalName)),
            '--name', (Quote-PowerShellValue $column.label),
            '--schemaName', (Quote-PowerShellValue $column.logicalName),
            '--type', (Quote-PowerShellValue $columnType),
            '--requiredLevel', (Quote-PowerShellValue (Convert-ToRequiredLevel -Required $column.required)),
            '--solution', (Quote-PowerShellValue $solutionName)
        )
        if ($column.type -eq 'choice' -and $column.choiceSet) {
            $command += @('--globalOptionSetName', (Quote-PowerShellValue (Get-DataverseLogicalName -SchemaName (Get-PublisherSchemaName -LogicalName $column.choiceSet))))
        }
        if ($column.maxLength -and $columnType -in @('String', 'Memo')) {
            $command += @('--len', (Quote-PowerShellValue $column.maxLength))
        }
        $steps.Add((New-CommandStep -Order $order -Phase 'columns' -Name ("{0}.{1}" -f $table.logicalName, $column.logicalName) -Command ($command -join ' ')))
        $order++
    }
}

foreach ($phaseName in @('mvp', 'future')) {
    foreach ($relationship in @($plan.relationshipPhases.$phaseName)) {
        $requiredLevel = if ([bool]$relationship.required) { 'ApplicationRequired' } else { 'None' }
        $deleteBehavior = if ($relationship.deleteBehavior -eq 'cascade') { 'RemoveLink' } else { 'Restrict' }
        $command = @(
            'pacx rel create n1',
            '--child', (Quote-PowerShellValue (Get-DataverseLogicalName -SchemaName $relationship.fromTable)),
            '--parent', (Quote-PowerShellValue (Get-DataverseLogicalName -SchemaName $relationship.toTable)),
            '--relName', (Quote-PowerShellValue $relationship.name),
            '--lookupSchemaName', (Quote-PowerShellValue $relationship.fromColumn),
            '--requiredLevel', (Quote-PowerShellValue $requiredLevel),
            '--cascadeDelete', (Quote-PowerShellValue $deleteBehavior),
            '--solution', (Quote-PowerShellValue $solutionName)
        ) -join ' '
        $steps.Add((New-CommandStep -Order $order -Phase "relationships/$phaseName" -Name $relationship.name -Command $command))
        $order++
    }
}

foreach ($orderedTable in @($plan.tableCreationOrder)) {
    if (-not $tableLookup.ContainsKey($orderedTable.logicalName)) {
        continue
    }
    $table = $tableLookup[$orderedTable.logicalName]
    foreach ($key in @($table.alternateKeys)) {
        $command = @(
            'pacx key create',
            '--table', (Quote-PowerShellValue (Get-DataverseLogicalName -SchemaName $table.logicalName)),
            '--name', (Quote-PowerShellValue $key.name),
            '--schemaName', (Quote-PowerShellValue $key.name),
            '--columns', (Quote-PowerShellValue ((Resolve-KeyColumns -Table $table -Columns @($key.columns)) -join ',')),
            '--solution', (Quote-PowerShellValue $solutionName)
        ) -join ' '
        $steps.Add((New-CommandStep -Order $order -Phase 'keys' -Name ("{0}.{1}" -f $table.logicalName, $key.name) -Command $command))
        $order++
    }
}

$columnCount = 0
$alternateKeyCount = 0
foreach ($table in $tables) {
    $columnCount += @($table.columns).Count
    $alternateKeyCount += @($table.alternateKeys).Count
}
$choiceCount = @($choices).Count
$tableCount = @($tables).Count
$relationshipCount = @($plan.relationshipPhases.mvp).Count + @($plan.relationshipPhases.future).Count
$bootstrapStepCount = $steps.Count

$source = [ordered]@{
    schemaBuildPlan = 'powerplatform/src/schema/schema-build-plan.json'
    dataverseTables = 'powerplatform/src/schema/dataverse-tables.yaml'
    choices = 'powerplatform/src/schema/choices.yaml'
}
$counts = [ordered]@{
    choices = $choiceCount
    tables = $tableCount
    columns = $columnCount
    alternateKeys = $alternateKeyCount
    relationships = $relationshipCount
    bootstrapSteps = $bootstrapStepCount
}
$manifest = New-Object System.Collections.Specialized.OrderedDictionary
$manifest.Add('generatedBy', [object]'scripts/Export-DataverseBootstrapArtifacts.ps1')
$manifest.Add('solutionName', [object]$solutionName)
$manifest.Add('source', [object]$source)
$manifest.Add('counts', [object]$counts)
$manifest['choices'] = [object[]]@($choices)
$manifest['tables'] = [object[]]@($tables)
$manifest['bootstrapSteps'] = [object[]]$steps.ToArray()

$manifestJson = $manifest | ConvertTo-Json -Depth 40
$scriptLines = @(
    '# Generated by scripts/Export-DataverseBootstrapArtifacts.ps1',
    '# Requires: pacx auth select <profile> before execution.',
    'param(',
    '    [string]$StartAt = ''''',
    ')',
    '',
    '$ErrorActionPreference = ''Stop''',
    '$script:StartAtReached = [string]::IsNullOrWhiteSpace($StartAt)',
    '',
    'function Invoke-PacxStep {',
    '    param(',
    '        [Parameter(Mandatory = $true)][string]$Label,',
    '        [Parameter(Mandatory = $true)][string]$Command,',
    '        [switch]$AllowAlreadyExists',
    '    )',
    '',
    '    Write-Host "==> $Label"',
    '    if (-not $script:StartAtReached) {',
    '        if ($Label -eq $StartAt) {',
    '            $script:StartAtReached = $true',
    '        } else {',
    '            Write-Host "Skipping before StartAt: $Label"',
    '            return',
    '        }',
    '    }',
    '',
    '    $global:LASTEXITCODE = 0',
    '    $output = Invoke-Expression $Command 2>&1',
    '    $exitCode = $LASTEXITCODE',
    '    $text = ($output | Out-String).TrimEnd()',
    '    if (-not [string]::IsNullOrWhiteSpace($text)) {',
    '        Write-Host $text',
    '    }',
    '',
    '    $failed = ($exitCode -ne 0) -or ($text -match ''(?im)\bFailed\b|CommandException|Exception of type'')',
    '    if ($failed) {',
    '        if ($AllowAlreadyExists -and $text -match ''(?im)already exists|already exist|duplicate'') {',
    '            Write-Host "Skipping existing component for $Label."',
    '            return',
    '        }',
    '        throw "PACX step failed: $Label"',
    '    }',
    '}',
    '',
    'Invoke-PacxStep -Label ''solution DecisionsWorkspace'' -Command ''pacx solution create --name ''''CareOps Decisions'''' --uniqueName ''''DecisionsWorkspace'''' --publisherPrefix ''''dft'''' --publisherUniqueName ''''CareOpsDecisions'''' --publisherFriendlyName ''''CareOps Decisions'''' --publisherOptionSetPrefix ''''60217'''''' -AllowAlreadyExists',
    ''
)
foreach ($step in $steps) {
    $scriptLines += "# $($step.order). [$($step.phase)] $($step.name)"
    $scriptLines += ("Invoke-PacxStep -Label {0} -Command {1} -AllowAlreadyExists" -f (Quote-PowerShellValue ("{0} {1}" -f $step.phase, $step.name)), (Quote-PowerShellValue $step.command))
    $scriptLines += ''
}
$scriptText = ($scriptLines -join "`n") + "`n"
$readmeText = @"
# Generated Dataverse Bootstrap Artifacts

These files are generated from `powerplatform/src/schema`.

- `bootstrap-manifest.json` is the machine-readable build manifest.
- `bootstrap.pacx.ps1` is the execution-oriented PACX script for a selected Dataverse environment.

Run `scripts/Export-DataverseBootstrapArtifacts.ps1 -ValidateOnly` to confirm the generated artifacts are current.
"@

$outputs = [ordered]@{
    (Join-Path $resolvedOutputRoot 'bootstrap-manifest.json') = $manifestJson + "`n"
    (Join-Path $resolvedOutputRoot 'bootstrap.pacx.ps1') = $scriptText
    (Join-Path $resolvedOutputRoot 'README.md') = $readmeText
}

if ($ValidateOnly) {
    foreach ($path in $outputs.Keys) {
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Generated Dataverse bootstrap artifact is missing: $path"
        }
        $existing = Read-Text -Path $path
        $expected = $outputs[$path]
        if ((Normalize-GeneratedArtifact -Path $path -Text $existing) -ne (Normalize-GeneratedArtifact -Path $path -Text $expected)) {
            throw "Generated Dataverse bootstrap artifact is stale: $path"
        }
    }
    Write-Host "Generated Dataverse bootstrap artifacts are current."
    return
}

if (-not (Test-Path -LiteralPath $resolvedOutputRoot)) {
    $null = New-Item -ItemType Directory -Path $resolvedOutputRoot -Force
}

$utf8 = New-Object System.Text.UTF8Encoding($false)
foreach ($path in $outputs.Keys) {
    [System.IO.File]::WriteAllText($path, $outputs[$path], $utf8)
    Write-Host "Wrote $path"
}

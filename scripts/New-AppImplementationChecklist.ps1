param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [string]$OutputJsonPath = 'powerplatform/src/app/implementation-checklist.json',
    [string]$OutputMarkdownPath = 'docs/deployment/app-implementation-checklist.md',
    [switch]$DryRun,
    [switch]$ValidateOnly
)

$ErrorActionPreference = 'Stop'

function Read-Text {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Missing file: $Path"
    }
    return [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $Path))
}

function Get-YamlMapKeys {
    param(
        [string]$Text,
        [string]$Section,
        [int]$Indent = 2
    )

    $lines = @($Text -split '\r?\n')
    $start = -1
    $sectionIndent = 0
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match ('^(?<indent>\s*)' + [regex]::Escape($Section) + ':\s*$')) {
            $start = $i
            $sectionIndent = $Matches.indent.Length
            break
        }
    }
    if ($start -lt 0) {
        return @()
    }

    $keys = New-Object System.Collections.Generic.List[string]
    $keyIndent = $sectionIndent + $Indent
    $keyPattern = '^' + (' ' * $keyIndent) + '(?<key>[A-Za-z0-9_]+):\s*$'
    for ($j = $start + 1; $j -lt $lines.Count; $j++) {
        if ($lines[$j] -match '^(?<indent>\s*)[A-Za-z0-9_]+:\s*$') {
            $currentIndent = $Matches.indent.Length
            if ($currentIndent -le $sectionIndent) {
                break
            }
        }
        if ($lines[$j] -match $keyPattern) {
            $keys.Add($Matches.key)
        }
    }
    return @($keys)
}

function Get-AppValue {
    param(
        [string]$Text,
        [string]$Key
    )
    $match = [regex]::Match($Text, ('(?m)^\s{2}' + [regex]::Escape($Key) + ':\s*(?<value>.+)$'))
    if ($match.Success) {
        return $match.Groups['value'].Value.Trim()
    }
    return $null
}

function New-ChecklistItem {
    param(
        [string]$Type,
        [string]$Name,
        [string[]]$Tasks,
        [string[]]$SourceRefs
    )
    [ordered]@{
        type = $Type
        name = $Name
        tasks = @($Tasks)
        sourceRefs = @($SourceRefs)
    }
}

function Convert-ToMarkdown {
    param([object]$Model)

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add('# App Implementation Checklist')
    $lines.Add('')
    $lines.Add('Generated: ' + ([datetime]::Parse($Model.generatedOn).ToString('yyyy-MM-dd HH:mm:ss zzz')))
    $lines.Add('')
    $lines.Add('## App')
    $lines.Add('')
    $lines.Add('- Name: ' + $Model.app.name)
    $lines.Add('- Runtime: ' + $Model.app.runtime)
    $lines.Add('- Storage target: ' + $Model.app.storageTarget)
    $lines.Add('')

    foreach ($group in @('screens', 'components', 'commands')) {
        $lines.Add('## ' + ($group.Substring(0, 1).ToUpper() + $group.Substring(1)))
        $lines.Add('')
        foreach ($item in @($Model.checklist.$group)) {
            $lines.Add('### ' + $item.name)
            $lines.Add('')
            foreach ($task in @($item.tasks)) {
                $lines.Add('- [ ] ' + $task)
            }
            $lines.Add('- Source refs: ' + (@($item.sourceRefs) -join ', '))
            $lines.Add('')
        }
    }

    return ($lines -join [System.Environment]::NewLine)
}

$repoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
$appMapPath = Join-Path $repoRoot 'powerplatform/src/app/app-map.yaml'
$componentsPath = Join-Path $repoRoot 'powerplatform/src/app/components.yaml'
$commandsPath = Join-Path $repoRoot 'powerplatform/src/app/commands.yaml'
$accessMatrixPath = Join-Path $repoRoot 'powerplatform/src/app/access-matrix.yaml'
$screensPath = Join-Path $repoRoot 'powerplatform/src/app/screens.md'

$appMapText = Read-Text -Path $appMapPath
$componentsText = Read-Text -Path $componentsPath
$commandsText = Read-Text -Path $commandsPath
Read-Text -Path $accessMatrixPath | Out-Null
Read-Text -Path $screensPath | Out-Null

$screens = Get-YamlMapKeys -Text $appMapText -Section 'screen_map' -Indent 2
$components = Get-YamlMapKeys -Text $componentsText -Section 'components' -Indent 2
$commands = Get-YamlMapKeys -Text $commandsText -Section 'commands' -Indent 2

if ($screens.Count -eq 0) { throw 'No screens found in app-map.yaml.' }
if ($components.Count -eq 0) { throw 'No components found in components.yaml.' }
if ($commands.Count -eq 0) { throw 'No commands found in commands.yaml.' }

$model = [ordered]@{
    schema = 'decisions.workspace.app-implementation-checklist.v1'
    generatedOn = (Get-Date).ToString('o')
    app = [ordered]@{
        name = Get-AppValue -Text $appMapText -Key 'name'
        runtime = Get-AppValue -Text $appMapText -Key 'runtime'
        storageTarget = Get-AppValue -Text $appMapText -Key 'storage_target'
    }
    checklist = [ordered]@{
        screens = @(
            foreach ($screen in $screens) {
                New-ChecklistItem -Type 'screen' -Name $screen -SourceRefs @('powerplatform/src/app/app-map.yaml', 'powerplatform/src/app/screens.md', 'powerplatform/src/app/access-matrix.yaml') -Tasks @(
                    "Create or verify the $screen canvas screen.",
                    'Bind the screen to the declared Dataverse data sources.',
                    'Apply role-aware visibility and command gating.',
                    'Capture final control names for Power Apps Test Engine selectors.'
                )
            }
        )
        components = @(
            foreach ($component in $components) {
                New-ChecklistItem -Type 'component' -Name $component -SourceRefs @('powerplatform/src/app/components.yaml') -Tasks @(
                    "Create or verify reusable component $component.",
                    'Keep inputs and outputs stable against the source contract.',
                    'Use the component only on declared screens.',
                    'Check read-only, busy, and empty states where applicable.'
                )
            }
        )
        commands = @(
            foreach ($command in $commands) {
                New-ChecklistItem -Type 'command' -Name $command -SourceRefs @('powerplatform/src/app/commands.yaml', 'powerplatform/src/app/access-matrix.yaml') -Tasks @(
                    "Create or verify command $command.",
                    'Wire OnSelect behavior to the documented Power Fx intent.',
                    'Gate command visibility by persona and selected record state.',
                    'Log connector-triggering actions where a flow is invoked.'
                )
            }
        )
    }
}

$json = $model | ConvertTo-Json -Depth 12
$markdown = Convert-ToMarkdown -Model $model

$outputJsonFullPath = if ([System.IO.Path]::IsPathRooted($OutputJsonPath)) { $OutputJsonPath } else { Join-Path $repoRoot $OutputJsonPath }
$outputMarkdownFullPath = if ([System.IO.Path]::IsPathRooted($OutputMarkdownPath)) { $OutputMarkdownPath } else { Join-Path $repoRoot $OutputMarkdownPath }

if ($ValidateOnly) {
    if (-not (Test-Path -LiteralPath $outputJsonFullPath)) { throw "Missing JSON checklist: $outputJsonFullPath" }
    if (-not (Test-Path -LiteralPath $outputMarkdownFullPath)) { throw "Missing markdown checklist: $outputMarkdownFullPath" }
    Get-Content -LiteralPath $outputJsonFullPath -Raw | ConvertFrom-Json | Out-Null
    Write-Host 'App implementation checklist is present and valid.'
    return
}

if (-not $DryRun) {
    $jsonDir = Split-Path -Parent $outputJsonFullPath
    $markdownDir = Split-Path -Parent $outputMarkdownFullPath
    if ($jsonDir -and -not (Test-Path -LiteralPath $jsonDir)) { New-Item -ItemType Directory -Path $jsonDir -Force | Out-Null }
    if ($markdownDir -and -not (Test-Path -LiteralPath $markdownDir)) { New-Item -ItemType Directory -Path $markdownDir -Force | Out-Null }
    Set-Content -LiteralPath $outputJsonFullPath -Value $json -Encoding utf8
    Set-Content -LiteralPath $outputMarkdownFullPath -Value $markdown -Encoding utf8
}

[pscustomobject]@{
    screens = $screens.Count
    components = $components.Count
    commands = $commands.Count
    dryRun = [bool]$DryRun
    outputJsonPath = $outputJsonFullPath
    outputMarkdownPath = $outputMarkdownFullPath
}

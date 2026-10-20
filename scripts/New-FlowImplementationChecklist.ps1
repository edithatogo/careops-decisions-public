param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [string]$OutputPath = 'docs/deployment/flow-implementation-checklist.md',
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

function ConvertFrom-FlowCatalogText {
    param([string]$Text)

    $catalog = [ordered]@{
        Schema      = $null
        Solution    = $null
        GeneratedOn = $null
        Flows       = @()
    }

    $current = $null
    $section = $null

    foreach ($rawLine in ($Text -split "`r?`n")) {
        $line = $rawLine.TrimEnd()

        if ($line -match '^schema:\s*(.+)$') {
            $catalog.Schema = $Matches[1]
            continue
        }

        if ($line -match '^solution:\s*(.+)$') {
            $catalog.Solution = $Matches[1]
            continue
        }

        if ($line -match '^generatedOn:\s*(.+)$') {
            $catalog.GeneratedOn = $Matches[1]
            continue
        }

        if ($line -match '^\s*-\s+name:\s*(.+)$') {
            if ($null -ne $current) {
                $catalog.Flows += [pscustomobject]$current
            }

            $current = [ordered]@{
                Name            = $Matches[1]
                File            = $null
                DisplayName     = $null
                Track           = $null
                Purpose         = $null
                Connectors      = @()
                EnvVars         = @()
                IdempotencyKey  = $null
                TestHook        = $null
            }
            $section = $null
            continue
        }

        if ($null -eq $current) {
            continue
        }

        if ($line -match '^\s+file:\s*(.+)$') {
            $current.File = $Matches[1]
            continue
        }

        if ($line -match '^\s+displayName:\s*(.+)$') {
            $current.DisplayName = $Matches[1]
            continue
        }

        if ($line -match '^\s+track:\s*(.+)$') {
            $current.Track = $Matches[1]
            continue
        }

        if ($line -match '^\s+purpose:\s*(.+)$') {
            $current.Purpose = $Matches[1]
            continue
        }

        if ($line -match '^\s+connectors:\s*$') {
            $section = 'connectors'
            continue
        }

        if ($line -match '^\s+envVars:\s*$') {
            $section = 'envVars'
            continue
        }

        if ($line -match '^\s+idempotencyKey:\s*(.+)$') {
            $current.IdempotencyKey = $Matches[1]
            continue
        }

        if ($line -match '^\s+testHook:\s*(.+)$') {
            $current.TestHook = $Matches[1]
            continue
        }

        if ($section -eq 'connectors' -and $line -match '^\s*-\s*(.+)$') {
            $current.Connectors += $Matches[1]
            continue
        }

        if ($section -eq 'envVars' -and $line -match '^\s*-\s*(.+)$') {
            $current.EnvVars += $Matches[1]
            continue
        }
    }

    if ($null -ne $current) {
        $catalog.Flows += [pscustomobject]$current
    }

    return [pscustomobject]$catalog
}

function Get-FlowImplementationSource {
    param([string]$RepoRoot)

    $jsonPath = Join-Path $RepoRoot 'powerplatform/src/flows/implementation-checklist.json'
    $yamlPath = Join-Path $RepoRoot 'powerplatform/src/flows/flow-catalog.yaml'

    if (Test-Path -LiteralPath $jsonPath) {
        $jsonText = Get-Content -LiteralPath $jsonPath -Raw
        if ($jsonText.Trim()) {
            return [pscustomobject]@{
                SourcePath = $jsonPath
                SourceKind = 'json'
                Data       = ($jsonText | ConvertFrom-Json)
            }
        }
    }

    if (-not (Test-Path -LiteralPath $yamlPath)) {
        throw ('Flow catalog not found: ' + $yamlPath)
    }

    return [pscustomobject]@{
        SourcePath = $yamlPath
        SourceKind = 'yaml'
        Data       = (ConvertFrom-FlowCatalogText -Text (Get-Content -LiteralPath $yamlPath -Raw))
    }
}

function Get-FlowEntries {
    param($SourceData)

    if ($SourceData.PSObject.Properties.Name -contains 'Flows') {
        return @($SourceData.Flows)
    }

    if ($SourceData.PSObject.Properties.Name -contains 'flows') {
        return @($SourceData.flows)
    }

    throw 'Unsupported flow implementation checklist source shape.'
}

function New-FlowSectionMarkdown {
    param([pscustomobject]$Flow)

    $lines = New-Object System.Collections.Generic.List[string]
    $connectors = @($Flow.Connectors)
    $envVars = @($Flow.EnvVars)

    [void]$lines.Add('### ' + $Flow.DisplayName)
    [void]$lines.Add('')
    [void]$lines.Add('- File: `' + $Flow.File + '`')
    if ($Flow.Track) {
        [void]$lines.Add('- Track: `' + $Flow.Track + '`')
    }
    if ($Flow.Purpose) {
        [void]$lines.Add('- Purpose: ' + $Flow.Purpose)
    }
    [void]$lines.Add('- Connectors: ' + ($(if ($connectors.Count -gt 0) { ($connectors -join ', ') } else { 'None listed' })))
    [void]$lines.Add('- Environment variables: ' + ($(if ($envVars.Count -gt 0) { ($envVars -join ', ') } else { 'None listed' })))
    [void]$lines.Add('- Idempotency key: `' + $Flow.IdempotencyKey + '`')
    [void]$lines.Add('- Test hook: `' + $Flow.TestHook + '`')
    [void]$lines.Add('')
    [void]$lines.Add('#### Maker implementation tasks')
    [void]$lines.Add('- [ ] Create or update the flow in Power Automate from `' + $Flow.File + '` without adding live write steps outside the intended business process.')
    [void]$lines.Add('- [ ] Configure the listed connectors and confirm the connection references match the environment.')
    [void]$lines.Add('- [ ] Bind the environment variables before test runs and keep values in deployment settings, not in the flow definition.')
    [void]$lines.Add('- [ ] Enforce idempotency with `' + $Flow.IdempotencyKey + '` so reruns do not duplicate side effects.')
    [void]$lines.Add('- [ ] Add a test path for `' + $Flow.TestHook + '` and verify it can be exercised without a live production change.')
    [void]$lines.Add('')
    [void]$lines.Add('#### Test hook checklist')
    [void]$lines.Add('- [ ] Run `' + $Flow.TestHook + '` against sample or replay data.')
    [void]$lines.Add('- [ ] Confirm the run is repeatable and does not create duplicate records.')
    [void]$lines.Add('- [ ] Confirm connector calls and environment variable resolution match the catalog entry.')
    [void]$lines.Add('- [ ] Capture any maker notes needed for packaging or deployment review.')
    [void]$lines.Add('')

    return ($lines -join [System.Environment]::NewLine)
}

function New-FlowImplementationMarkdown {
    param(
        [pscustomobject]$Source,
        [string]$RepoRoot
    )

    $catalogData = $Source.Data
    $flows = Get-FlowEntries -SourceData $catalogData

    $lines = New-Object System.Collections.Generic.List[string]
    [void]$lines.Add('# Flow Implementation Checklist')
    [void]$lines.Add('')
    [void]$lines.Add('Generated checklist for the current Decisions Workspace flow catalog.')
    [void]$lines.Add('')
    [void]$lines.Add('Source: `powerplatform/src/flows/flow-catalog.yaml`')
    [void]$lines.Add('Checklist source: `' + $Source.SourceKind + '` at `powerplatform/src/flows/implementation-checklist.json`')
    [void]$lines.Add('')
    [void]$lines.Add('Use the generator to refresh this file from the repo state:')
    [void]$lines.Add('')
    [void]$lines.Add('```powershell')
    [void]$lines.Add('powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-FlowImplementationChecklist.ps1')
    [void]$lines.Add('```')
    [void]$lines.Add('')
    [void]$lines.Add('## Flow Summary')
    [void]$lines.Add('')
    [void]$lines.Add('| Flow | Connectors | Environment variables | Idempotency key | Test hook |')
    [void]$lines.Add('| --- | --- | --- | --- | --- |')

    foreach ($flow in $flows) {
        $connectors = if (@($flow.Connectors).Count -gt 0) { (@($flow.Connectors) -join '<br>') } else { 'None listed' }
        $envVars = if (@($flow.EnvVars).Count -gt 0) { (@($flow.EnvVars) -join '<br>') } else { 'None listed' }
        [void]$lines.Add('| ' + $flow.DisplayName + ' | ' + $connectors + ' | ' + $envVars + ' | `' + $flow.IdempotencyKey + '` | `' + $flow.TestHook + '` |')
    }

    [void]$lines.Add('')
    [void]$lines.Add('## Per-flow checklist')
    [void]$lines.Add('')

    foreach ($flow in $flows) {
        [void]$lines.Add((New-FlowSectionMarkdown -Flow $flow))
    }

    [void]$lines.Add('## Safe verification order')
    [void]$lines.Add('')
    [void]$lines.Add('1. Review the flow catalog and checklist output.')
    [void]$lines.Add('2. Configure connectors and environment variables in the maker environment.')
    [void]$lines.Add('3. Validate idempotency and test hook behavior with sample data.')
    [void]$lines.Add('4. Package only after the local checklist is complete.')
    [void]$lines.Add('')
    [void]$lines.Add('```powershell')
    [void]$lines.Add('powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-FlowImplementationChecklist.ps1 -DryRun')
    [void]$lines.Add('```')

    return ($lines -join [System.Environment]::NewLine)
}

$source = Get-FlowImplementationSource -RepoRoot $RepoRoot
$markdown = New-FlowImplementationMarkdown -Source $source -RepoRoot $RepoRoot
$outputFullPath = if ([System.IO.Path]::IsPathRooted($OutputPath)) { $OutputPath } else { Join-Path $RepoRoot $OutputPath }

if (-not $DryRun) {
    $outputDir = Split-Path -Parent $outputFullPath
    if ($outputDir -and -not (Test-Path -LiteralPath $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }

    Set-Content -LiteralPath $outputFullPath -Value $markdown -Encoding utf8
}

[pscustomobject]@{
    OutputPath = $outputFullPath
    DryRun     = [bool]$DryRun
    SourceKind = $source.SourceKind
    SourcePath = $source.SourcePath
    FlowCount  = @((Get-FlowEntries -SourceData $source.Data)).Count
    Markdown   = $markdown
}

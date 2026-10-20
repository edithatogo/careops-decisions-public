param(
    [switch]$Strict
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$failures = New-Object System.Collections.Generic.List[string]

function Get-RelativePathText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BasePath,
        [Parameter(Mandatory = $true)]
        [string]$TargetPath
    )

    $baseFull = [System.IO.Path]::GetFullPath($BasePath).TrimEnd('\', '/')
    $targetFull = [System.IO.Path]::GetFullPath($TargetPath)

    if ($targetFull.StartsWith($baseFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        $relative = $targetFull.Substring($baseFull.Length).TrimStart('\', '/')
        if ([string]::IsNullOrWhiteSpace($relative)) {
            return '.'
        }
        return $relative
    }

    return $TargetPath
}

function Test-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    try {
        $null = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -ErrorAction Stop
        return $true
    } catch {
        $failures.Add("Invalid JSON: $(Get-RelativePathText -BasePath $root -TargetPath $Path) - $($_.Exception.Message)")
        return $false
    }
}

function Get-MeaningfulYamlLines {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    foreach ($rawLine in Get-Content -LiteralPath $Path) {
        $line = $rawLine.TrimEnd()
        if ($line -match '^\s*$') { continue }
        if ($line -match '^\s*#') { continue }
        $line
    }
}

function Test-YamlStructure {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [ValidateSet('testplan', 'suite')]
        [string]$Kind
    )

    $relative = Get-RelativePathText -BasePath $root -TargetPath $Path
    $lines = @(Get-MeaningfulYamlLines -Path $Path)

    if ($lines.Count -eq 0) {
        $failures.Add("Empty YAML file: $relative")
        return
    }

    if ($lines -match "`t") {
        $failures.Add("YAML file uses tab indentation: $relative")
    }

    if ($Kind -eq 'testplan') {
        foreach ($key in @('name:', 'displayName:', 'status:', 'app:', 'suites:')) {
            $pattern = '^' + [regex]::Escape($key)
            if (-not ($lines -match $pattern)) {
                $failures.Add("Missing expected top-level key '$key' in $relative")
            }
        }

        $suiteRefs = $lines | Where-Object { $_ -match '^\s*-\s+name:\s+' }
        if ($suiteRefs.Count -lt 4) {
            $failures.Add("Expected four suite entries in $relative")
        }

        $content = Get-Content -LiteralPath $Path -Raw
        foreach ($pattern in @(
            'governance-body.yaml',
            'meeting-agenda.yaml',
            'minutes-decisions-actions.yaml',
            'integrations.yaml',
            'create and maintain governance bodies',
            'create meetings and build agendas',
            'capture minutes, decisions, and actions',
            'verify connector-heavy integration placeholders'
        )) {
            if ($content -notmatch [regex]::Escape($pattern)) {
                $failures.Add("Missing expected test plan content '$pattern' in $relative")
            }
        }
        return
    }

    foreach ($key in @('name:', 'displayName:', 'status:', 'intent:', 'selectors:', 'tests:')) {
        $pattern = '^' + [regex]::Escape($key)
        if (-not ($lines -match $pattern)) {
            $failures.Add("Missing expected top-level key '$key' in $relative")
        }
    }

    if (-not ($lines -match '^\s+screen:\s+')) {
        $failures.Add("Missing a screen selector in $relative")
    }

    if (-not ($lines -match '^\s+-\s+id:\s+')) {
        $failures.Add("Missing test case entries in $relative")
    }

    $content = Get-Content -LiteralPath $Path -Raw
    switch -Wildcard ($relative) {
        '*governance-body.yaml' {
            foreach ($pattern in @('GB-01', 'GB-02', 'scrGovernanceBody', 'btnSaveGovernanceBody')) {
                if ($content -notmatch [regex]::Escape($pattern)) {
                    $failures.Add("Missing expected governance body content '$pattern' in $relative")
                }
            }
        }
        '*meeting-agenda.yaml' {
            foreach ($pattern in @('MA-01', 'MA-02', 'MA-03', 'scrMeetingAgenda', 'btnAddAgendaItem', 'txtPaperTitle')) {
                if ($content -notmatch [regex]::Escape($pattern)) {
                    $failures.Add("Missing expected meeting agenda content '$pattern' in $relative")
                }
            }
        }
        '*minutes-decisions-actions.yaml' {
            foreach ($pattern in @('MDA-01', 'MDA-02', 'MDA-03', 'scrMinutesDecisionsActions', 'btnAddDecision', 'btnApproveMinutes', 'cmbMinutesApprovalStatus')) {
                if ($content -notmatch [regex]::Escape($pattern)) {
                    $failures.Add("Missing expected minutes content '$pattern' in $relative")
                }
            }
        }
        '*integrations.yaml' {
            foreach ($pattern in @('INT-01', 'INT-02', 'INT-03', 'scrIntegrations', 'btnRefreshIntegrationStatus', 'btnSyncActionsToPlanner')) {
                if ($content -notmatch [regex]::Escape($pattern)) {
                    $failures.Add("Missing expected integration content '$pattern' in $relative")
                }
            }
            if ($content -notmatch 'no live connector calls are expected') {
                $failures.Add("Missing connector-placeholder disclaimer in $relative")
            }
        }
    }
}

Write-Host "Validating JSON files..."
$jsonFiles = Get-ChildItem -LiteralPath $root -Recurse -File -Filter '*.json'
foreach ($file in $jsonFiles) {
    $null = Test-JsonFile -Path $file.FullName
}

Write-Host "Validating Power Apps YAML skeleton..."
$testPlan = Join-Path $root 'tests/powerapps/DecisionsWorkspace.testplan.yaml'
$suiteFiles = @(
    (Join-Path $root 'tests/powerapps/suites/governance-body.yaml'),
    (Join-Path $root 'tests/powerapps/suites/meeting-agenda.yaml'),
    (Join-Path $root 'tests/powerapps/suites/minutes-decisions-actions.yaml'),
    (Join-Path $root 'tests/powerapps/suites/integrations.yaml')
)

if (-not (Test-Path -LiteralPath $testPlan)) {
    $failures.Add("Missing YAML file: tests/powerapps/DecisionsWorkspace.testplan.yaml")
} else {
    Test-YamlStructure -Path $testPlan -Kind testplan
}

foreach ($suite in $suiteFiles) {
    if (-not (Test-Path -LiteralPath $suite)) {
        $failures.Add("Missing YAML file: $(Get-RelativePathText -BasePath $root -TargetPath $suite)")
        continue
    }
    Test-YamlStructure -Path $suite -Kind suite
}

if ($Strict) {
    $yamlPaths = @($testPlan) + $suiteFiles
    foreach ($yaml in $yamlPaths) {
        if (Test-Path -LiteralPath $yaml) {
            $content = Get-Content -LiteralPath $yaml -Raw
            if ($content -notmatch '(?m)^\s*status:\s+') {
                $failures.Add("Missing status field in $(Get-RelativePathText -BasePath $root -TargetPath $yaml)")
            }
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "JSON and YAML validation passed."

param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [string]$OutputPath = 'docs/deployment/deployment-checklist.md',
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

function New-ChecklistMarkdown {
    param([pscustomobject]$Readiness)

    $lines = New-Object System.Collections.Generic.List[string]
    [void]$lines.Add("# Deployment Checklist")
    [void]$lines.Add("")
    $generated = [datetimeoffset]$Readiness.GeneratedOn
    [void]$lines.Add('Generated: ' + $generated.ToString('yyyy-MM-dd HH:mm:ss zzz'))
    [void]$lines.Add("")
    [void]$lines.Add('Readiness result: **' + $Readiness.Readiness + '**')
    [void]$lines.Add("")
    [void]$lines.Add("## Checklist")
    [void]$lines.Add("")

    foreach ($check in $Readiness.Checks) {
        $mark = if ($check.Status -eq 'Pass') { 'x' } else { ' ' }
        [void]$lines.Add('- [' + $mark + '] ' + $check.Name)
        if ($check.Details) {
            [void]$lines.Add('  - ' + $check.Details)
        }
        if ($check.Recommendation) {
            [void]$lines.Add('  - Next: ' + $check.Recommendation)
        }
    }

    [void]$lines.Add("")
    [void]$lines.Add("## Safe Command Order")
    [void]$lines.Add("")
    [void]$lines.Add("1. Review readiness output.")
    [void]$lines.Add("2. Pack the unmanaged solution.")
    [void]$lines.Add("3. Run solution check on the generated zip.")
    [void]$lines.Add("4. Import only when the target environment and connection references are confirmed.")
    [void]$lines.Add("")
    [void]$lines.Add('```powershell')
    [void]$lines.Add('powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Get-EnvironmentReadiness.ps1')
    [void]$lines.Add('pac solution pack --folder powerplatform/solutions/DecisionsWorkspace --zipfile artifacts/solutions/DecisionsWorkspace_unmanaged.zip --packagetype Unmanaged')
    [void]$lines.Add('pac solution check --path artifacts/solutions/DecisionsWorkspace_unmanaged.zip')
    [void]$lines.Add('pac solution import --path artifacts/solutions/DecisionsWorkspace_unmanaged.zip --settings-file powerplatform/settings/deployment-settings.sample.json --publish-changes')
    [void]$lines.Add('```')

    return ($lines -join [System.Environment]::NewLine)
}

$readinessScript = Join-Path $PSScriptRoot 'Get-EnvironmentReadiness.ps1'
if (-not (Test-Path -LiteralPath $readinessScript)) {
    throw ('Readiness script not found: ' + $readinessScript)
}

$readiness = (& $readinessScript -RepoRoot $RepoRoot -Json | ConvertFrom-Json)

$outputFullPath = if ([System.IO.Path]::IsPathRooted($OutputPath)) { $OutputPath } else { Join-Path $RepoRoot $OutputPath }
$markdown = New-ChecklistMarkdown -Readiness $readiness

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
    Readiness  = $readiness.Readiness
    Markdown   = $markdown
}

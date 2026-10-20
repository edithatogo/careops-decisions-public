[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [Parameter(Mandatory = $true)]
    [ValidateSet('development', 'test', 'enterprise')]
    [string]$Environment,
    [Parameter(Mandatory = $true)]
    [string]$Summary,
    [Parameter(Mandatory = $false)]
    [string[]]$Features,
    [Parameter(Mandatory = $false)]
    [string[]]$FixedIssues,
    [Parameter(Mandatory = $false)]
    [string[]]$Changes,
    [Parameter(Mandatory = $false)]
    [string[]]$KnownIssues,
    [Parameter(Mandatory = $false)]
    [ValidateSet('Unmanaged', 'Managed')]
    [string]$PackageType = 'Unmanaged',
    [Parameter(Mandatory = $false)]
    [ValidateSet('Passed', 'Not Run', 'Not Applicable')]
    [string]$SolutionCheckerStatus = 'Not Run',
    [Parameter(Mandatory = $false)]
    [ValidateSet('Low', 'Medium', 'High')]
    [string]$RollbackRisk = 'Low',
    [Parameter(Mandatory = $false)]
    [string]$OutputPath,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

if (-not ($Version -match '^\d+\.\d+\.\d+\.\d+$')) {
    throw "Version must be a four-part semver string (e.g. 1.0.0.1). Got: $Version"
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $repoRoot = Split-Path -Parent $PSScriptRoot
    $OutputPath = Join-Path $repoRoot "artifacts/release-notes/RELEASE_NOTES_$Version.md"
}

$outputDir = Split-Path -Parent $OutputPath -Resolve -ErrorAction SilentlyContinue
if (-not $outputDir) {
    $outputDir = Split-Path -Parent $OutputPath
    $null = New-Item -ItemType Directory -Path $outputDir -Force
}

if ((Test-Path -LiteralPath $OutputPath) -and -not $Force) {
    throw "Release notes already exist at $OutputPath. Use -Force to overwrite."
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Release $Version")
$lines.Add('')
$lines.Add("**Date:** $(Get-Date -Format 'yyyy-MM-dd')")
$lines.Add("**Environment:** $Environment")
$lines.Add("**Solution:** CareOps Decisions (DecisionsWorkspace) $PackageType")
$lines.Add('')
$lines.Add('## Summary')
$lines.Add('')
$lines.Add($Summary)
$lines.Add('')

if ($Features -and $Features.Count -gt 0) {
    $lines.Add('## New Features')
    $lines.Add('')
    foreach ($f in $Features) { $lines.Add("- $f") }
    $lines.Add('')
}

if ($FixedIssues -and $FixedIssues.Count -gt 0) {
    $lines.Add('## Fixed Issues')
    $lines.Add('')
    foreach ($i in $FixedIssues) { $lines.Add("- $i") }
    $lines.Add('')
}

if ($Changes -and $Changes.Count -gt 0) {
    $lines.Add('## Changes')
    $lines.Add('')
    foreach ($c in $Changes) { $lines.Add("- $c") }
    $lines.Add('')
}

$lines.Add('## Deployment Profile')
$lines.Add('')
$lines.Add("- **Profile:** $Environment")
$lines.Add("- **Package Type:** $PackageType")
$lines.Add("- **Solution Checker:** $SolutionCheckerStatus")
$lines.Add('')

if ($KnownIssues -and $KnownIssues.Count -gt 0) {
    $lines.Add('## Known Issues')
    $lines.Add('')
    foreach ($k in $KnownIssues) { $lines.Add("- $k") }
    $lines.Add('')
}

$lines.Add('## Rollback Notes')
$lines.Add('')
$lines.Add('- **Rollback method:** Import previous version or restore from backup')
$lines.Add("- **Rollback risk:** $RollbackRisk")
$lines.Add('- **Data impact:** Describe any data that could be lost on rollback')
$lines.Add('- **Rollback tested:** No')

$content = $lines -join "`r`n"
[System.IO.File]::WriteAllText($OutputPath, $content, [System.Text.UTF8Encoding]($false))

Write-Host "Release notes generated: $OutputPath"
Write-Host "Summary: $Version for $Environment ($PackageType, checker: $SolutionCheckerStatus)"

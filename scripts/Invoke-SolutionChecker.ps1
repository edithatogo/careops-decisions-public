param(
    [string]$SolutionZip = "powerplatform/solutions/DecisionsWorkspace.zip",
    [string]$OutputDirectory = "output/solution-checker",
    [switch]$FailOnHighSeverity
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not (Get-Command pac -ErrorAction SilentlyContinue)) {
    throw "Power Platform CLI 'pac' was not found on PATH."
}

if (-not (Test-Path -LiteralPath $SolutionZip)) {
    throw "Solution zip not found: $SolutionZip. Run scripts/Pack-Solution.ps1 or export a live solution first."
}

[System.IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null

$args = @(
    'solution',
    'check',
    '--path',
    $SolutionZip,
    '--outputDirectory',
    $OutputDirectory
)

Write-Host "Running pac $($args -join ' ')"
pac @args

if ($FailOnHighSeverity) {
    Write-Host "High severity fail gate requested. Review solution checker output in $OutputDirectory."
}

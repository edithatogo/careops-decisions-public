param(
    [string]$SolutionZip = "artifacts/solutions/DecisionsWorkspace/unmanaged/DecisionsWorkspace_unmanaged.zip",
    [string]$SolutionSourcePath = "artifacts/roundtrip/DecisionsWorkspace",
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command pac -ErrorAction SilentlyContinue)) {
    throw "Power Platform CLI 'pac' was not found on PATH."
}

if (-not (Test-Path -LiteralPath $SolutionZip)) {
    throw "Solution zip not found: $SolutionZip"
}

$arguments = @('solution', 'unpack', '--zipfile', $SolutionZip, '--folder', $SolutionSourcePath, '--packagetype', 'Unmanaged')
if ($DryRun) {
    Write-Host ("Dry run: pac {0}" -f ($arguments -join ' '))
    return
}

pac @arguments
if ($LASTEXITCODE -ne 0) {
    throw "PAC solution unpack failed for $SolutionZip."
}

param(
    [string]$SolutionSourcePath = "powerplatform/solutions/DecisionsWorkspace",
    [string]$OutputZip = "powerplatform/solutions/DecisionsWorkspace.zip"
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command pac -ErrorAction SilentlyContinue)) {
    throw "Power Platform CLI 'pac' was not found on PATH."
}

if (-not (Test-Path -LiteralPath $SolutionSourcePath)) {
    throw "Solution source path not found: $SolutionSourcePath"
}

pac solution pack --folder $SolutionSourcePath --zipfile $OutputZip --packagetype Unmanaged

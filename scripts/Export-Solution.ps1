param(
    [Parameter(Mandatory = $true)]
    [string]$SolutionName,
    [string]$OutputZip = "powerplatform/solutions/DecisionsWorkspace.zip",
    [switch]$Managed
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command pac -ErrorAction SilentlyContinue)) {
    throw "Power Platform CLI 'pac' was not found on PATH."
}

$managedFlag = if ($Managed) { "--managed" } else { "" }
pac solution export --name $SolutionName --path $OutputZip $managedFlag

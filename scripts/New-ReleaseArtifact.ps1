[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$SolutionName = "DecisionsWorkspace",
    [string]$SolutionSourcePath = "powerplatform/solutions/DecisionsWorkspace",
    [string]$OutputRoot = "artifacts/solutions",
    [switch]$Managed,
    [switch]$Unmanaged,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

function Resolve-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PathValue
    )

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return $PathValue
    }

    return Join-Path $repoRoot $PathValue
}

function Resolve-PacCommand {
    $actionPacPath = $env:POWERPLATFORMTOOLS_PACPATH
    if ($actionPacPath -and (Test-Path -LiteralPath $actionPacPath)) {
        return $actionPacPath
    }

    $pathPac = Get-Command pac -ErrorAction SilentlyContinue
    if ($pathPac) {
        return $pathPac.Source
    }

    return $null
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$solutionSourceFullPath = Resolve-RepoPath $SolutionSourcePath
$outputRootFullPath = Resolve-RepoPath $OutputRoot
$simulation = $DryRun
$pacCommand = Resolve-PacCommand

if (-not $simulation -and -not $pacCommand) {
    throw "Power Platform CLI 'pac' was not found on PATH or POWERPLATFORMTOOLS_PACPATH."
}

if (-not (Test-Path -LiteralPath $solutionSourceFullPath)) {
    throw "Solution source path not found: $solutionSourceFullPath"
}

if (-not $Managed -and -not $Unmanaged) {
    $Managed = $true
    $Unmanaged = $true
}

$packageTypes = @()
if ($Managed) {
    $packageTypes += 'Managed'
}
if ($Unmanaged) {
    $packageTypes += 'Unmanaged'
}

$results = @()

foreach ($packageType in $packageTypes) {
    $artifactFolder = Join-Path $outputRootFullPath (Join-Path $SolutionName ($packageType.ToLowerInvariant()))
    $artifactName = "{0}_{1}.zip" -f $SolutionName, $packageType.ToLowerInvariant()
    $artifactPath = Join-Path $artifactFolder $artifactName
    $commandLabel = if ($pacCommand) { $pacCommand } else { 'pac' }
    $command = "$commandLabel solution pack --folder `"$solutionSourceFullPath`" --zipfile `"$artifactPath`" --packagetype $packageType"

    $results += [pscustomobject]@{
        SolutionName = $SolutionName
        PackageType   = $packageType
        ArtifactPath  = $artifactPath
        Command       = $command
        Status        = if ($simulation) { 'Planned' } else { 'Pending' }
    }

    if ($simulation) {
        Write-Host "Dry run: $command"
        continue
    }

    if (-not $PSCmdlet.ShouldProcess($artifactPath, "Pack $packageType release artifact")) {
        continue
    }

    New-Item -ItemType Directory -Path $artifactFolder -Force | Out-Null
    Write-Host "Packing $packageType release artifact to $artifactPath"
    & $pacCommand solution pack --folder $solutionSourceFullPath --zipfile $artifactPath --packagetype $packageType
    if ($LASTEXITCODE -ne 0) {
        throw "PAC solution pack failed for $packageType at $artifactPath"
    }

    $results[-1].Status = 'Created'
}

$results

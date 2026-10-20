param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$solutionRoot = Join-Path $repoRoot 'powerplatform/solutions/DecisionsWorkspace'
$versionFile = Join-Path $repoRoot 'VERSION'
$failures = New-Object System.Collections.Generic.List[string]

function Resolve-SolutionSourceRoot {
    param([Parameter(Mandatory = $true)][string]$SolutionRoot)

    foreach ($candidate in @($SolutionRoot, (Join-Path $SolutionRoot 'src'))) {
        if (Test-Path -LiteralPath (Join-Path $candidate 'Other/Solution.xml')) {
            return $candidate
        }
    }

    return $SolutionRoot
}

$solutionSourceRoot = Resolve-SolutionSourceRoot -SolutionRoot $solutionRoot

$requiredFiles = @(
    'Other/Solution.xml',
    'Other/Customizations.xml',
    'Other/Relationships.xml'
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $solutionSourceRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $displayPath = $path.Substring($repoRoot.Length).TrimStart('\', '/')
        $failures.Add("Missing PAC solution bootstrap file: $displayPath")
        continue
    }

    if ((Get-Item -LiteralPath $path).Length -eq 0) {
        $displayPath = $path.Substring($repoRoot.Length).TrimStart('\', '/')
        $failures.Add("Empty PAC solution bootstrap file: $displayPath")
    }
}

if (Test-Path -LiteralPath (Join-Path $solutionSourceRoot 'Other/Solution.xml')) {
    [xml]$solution = Get-Content -LiteralPath (Join-Path $solutionSourceRoot 'Other/Solution.xml') -Raw
    $manifest = $solution.ImportExportXml.SolutionManifest

    if ([string]$manifest.UniqueName -notin @('DecisionsWorkspace', 'decisionsworkspace')) {
        $failures.Add("Solution unique name must be DecisionsWorkspace or decisionsworkspace.")
    }

    if ($manifest.Publisher.CustomizationPrefix -ne 'dft') {
        $failures.Add("Solution publisher prefix must be dft.")
    }

    if (Test-Path -LiteralPath $versionFile) {
        $repoVersion = (Get-Content -LiteralPath $versionFile -Raw).Trim()
        $solutionVersion = [string]$manifest.Version
        if ($solutionVersion -notmatch "^$([regex]::Escape($repoVersion))(\.0)?$") {
            $failures.Add("Solution version '$solutionVersion' does not align with repo VERSION '$repoVersion'.")
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "PAC solution bootstrap validation completed."

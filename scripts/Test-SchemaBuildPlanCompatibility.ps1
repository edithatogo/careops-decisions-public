param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$schemaRoot = Join-Path $repoRoot 'powerplatform/src/schema'
$exporter = Join-Path $repoRoot 'scripts/Export-SchemaBuildPlan.ps1'
$outputPath = Join-Path $schemaRoot 'schema-build-plan.json'
$scratchRoot = Join-Path $repoRoot 'tmp/schema-build-plan-compat'

$schemaFiles = @(
    'dataverse-tables.yaml',
    'choices.yaml',
    'relationships.yaml'
)

function Write-NormalizedText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourcePath,

        [Parameter(Mandatory = $true)]
        [string]$TargetPath,

        [Parameter(Mandatory = $true)]
        [ValidateSet('LF', 'CRLF')]
        [string]$LineEnding
    )

    $text = [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $SourcePath))
    $normalized = (($text -replace "`r`n", "`n") -replace "`r", "`n").TrimEnd("`n")
    $targetText = if ($LineEnding -eq 'CRLF') {
        ($normalized -replace "`n", "`r`n") + "`r`n"
    } else {
        $normalized + "`n"
    }

    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($TargetPath, $targetText, $encoding)
}

if (-not (Test-Path -LiteralPath $exporter)) {
    throw "Missing schema exporter: $exporter"
}

if (Test-Path -LiteralPath $scratchRoot) {
    Remove-Item -LiteralPath $scratchRoot -Recurse -Force
}

try {
    foreach ($lineEnding in @('LF', 'CRLF')) {
        $caseRoot = Join-Path $scratchRoot $lineEnding.ToLowerInvariant()
        $null = New-Item -ItemType Directory -Path $caseRoot -Force

        foreach ($fileName in $schemaFiles) {
            $source = Join-Path $schemaRoot $fileName
            if (-not (Test-Path -LiteralPath $source)) {
                throw "Missing schema source file: $source"
            }

            $target = Join-Path $caseRoot $fileName
            Write-NormalizedText -SourcePath $source -TargetPath $target -LineEnding $lineEnding
        }

        & $exporter -SchemaRoot $caseRoot -OutputPath $outputPath -ValidateOnly
        Write-Host "Schema build plan compatibility passed for $lineEnding input."
    }
}
finally {
    if (Test-Path -LiteralPath $scratchRoot) {
        Remove-Item -LiteralPath $scratchRoot -Recurse -Force
    }
}

Write-Host "Schema build plan compatibility validation completed."

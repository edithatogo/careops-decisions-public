[CmdletBinding()]
param(
    [Parameter(Mandatory)] [ValidatePattern('^[a-f0-9]{40}$')] [string] $SourceCommit,
    [Parameter(Mandatory)] [string] $Archive,
    [Parameter(Mandatory)] [string] $ConsumerDirectory,
    [Parameter(Mandatory)] [string] $ResultsDirectory
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (Test-Path $ConsumerDirectory) { throw 'Existing consumer evidence must be retained.' }
Expand-Archive -LiteralPath $Archive -DestinationPath $ConsumerDirectory
$manifest = Get-Content -LiteralPath (Join-Path $ConsumerDirectory 'native-release.json') -Raw | ConvertFrom-Json
if ($manifest.sourceCommit -cne $SourceCommit) { throw 'Unpacked package source identity mismatch.' }
$cases = @(
    @{ Directory = 'native_authority'; Assembly = 'Authority.Tests.exe'; Minimum = 22 },
    @{ Directory = 'native_calendar'; Assembly = 'Calendars.Tests.exe'; Minimum = 22 },
    @{ Directory = 'native_identity'; Assembly = 'Identity.Tests.exe'; Minimum = 57 },
    @{ Directory = 'native_register_schema'; Assembly = 'RegisterSchema.Tests.exe'; Minimum = 12 }
)
New-Item -ItemType Directory -Path $ResultsDirectory -Force | Out-Null
$results = @()
foreach ($case in $cases) {
    # Only the test harness/dependencies come from build output. Every project
    # library used by that harness is replaced with the unpacked release bytes.
    $testBin = Join-Path $root "tests/$($case.Directory)/bin/Release/net48"
    $consumer = Join-Path $ConsumerDirectory $case.Directory
    Copy-Item -LiteralPath $testBin -Destination $consumer -Recurse
    $replaced = @()
    foreach ($assembly in $manifest.assemblies) {
        $target = Join-Path $consumer "$($assembly.name).dll"
        if (Test-Path -LiteralPath $target) {
            Copy-Item -LiteralPath (Join-Path $ConsumerDirectory "lib/$($assembly.name).dll") -Destination $target -Force
            if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash.ToLowerInvariant() -cne $assembly.sha256) { throw 'Unpacked consumer assembly hash mismatch.' }
            $replaced += $assembly.name
        }
    }
    if ($replaced.Count -eq 0) { throw 'Consumer did not load any packaged project library.' }
    Push-Location $consumer
    try {
        dotnet test (Join-Path $consumer $case.Assembly) --test-adapter-path $consumer --logger "trx;LogFileName=$($case.Directory).trx" --results-directory $ResultsDirectory
        if ($LASTEXITCODE -ne 0) { throw "Unpacked native consumer failed: $($case.Directory)" }
    } finally { Pop-Location }
    [xml] $trx = Get-Content -LiteralPath (Join-Path $ResultsDirectory "$($case.Directory).trx") -Raw
    $counts = $trx.TestRun.ResultSummary.Counters
    if ([int]$counts.total -lt $case.Minimum -or [int]$counts.executed -ne [int]$counts.total -or [int]$counts.passed -ne [int]$counts.total) { throw 'All real consumer tests must execute and pass without skips.' }
    $results += @{ suite = $case.Directory; passed = [int]$counts.passed; packagedAssemblies = $replaced }
}
@{ sourceCommit = $manifest.sourceCommit; suites = $results; nativeWrites = $false } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $ResultsDirectory 'consumer.json') -Encoding utf8

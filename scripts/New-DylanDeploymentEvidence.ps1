[CmdletBinding()]
param(
    [string]$EnvironmentUrl = "https://environment.example.invalid/",
    [string]$AppId = "22222222-2222-4222-8222-222222222222",
    [string]$AppUniqueName = "decisions_workspace_64f7b61a",
    [string]$QuickStartPageName = "Quick Start Guide",
    [int]$PacTimeoutSeconds = 30,
    [switch]$CheckMedicationSafetySample,
    [string]$OutputDirectory = "artifacts/deployment"
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$resolvedOutputDirectory = Join-Path $repoRoot $OutputDirectory
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$jsonPath = Join-Path $resolvedOutputDirectory "dylan-verification-$timestamp.json"
$markdownPath = Join-Path $resolvedOutputDirectory "dylan-verification-$timestamp.md"
$verifier = Join-Path $PSScriptRoot 'Verify-DylanDeployment.ps1'

if (-not (Test-Path -LiteralPath $verifier)) {
    throw "Verifier script was not found: $verifier"
}

New-Item -ItemType Directory -Path $resolvedOutputDirectory -Force | Out-Null

$arguments = @(
    '-NoProfile',
    '-ExecutionPolicy',
    'Bypass',
    '-File',
    $verifier,
    '-EnvironmentUrl',
    $EnvironmentUrl,
    '-AppId',
    $AppId,
    '-AppUniqueName',
    $AppUniqueName,
    '-QuickStartPageName',
    $QuickStartPageName,
    '-PacTimeoutSeconds',
    $PacTimeoutSeconds,
    '-Json'
)

if ($CheckMedicationSafetySample) {
    $arguments += '-CheckMedicationSafetySample'
}

$rawOutput = & powershell @arguments 2>&1
$exitCode = $LASTEXITCODE
$rawText = ($rawOutput | Out-String).Trim()

if ([string]::IsNullOrWhiteSpace($rawText)) {
    $summary = [pscustomobject]@{
        environmentUrl = $EnvironmentUrl
        appId = $AppId
        appUniqueName = $AppUniqueName
        generatedOn = (Get-Date).ToString('o')
        passed = 0
        failed = 1
        checks = @(
            [pscustomobject]@{
                name = 'verifier-output'
                passed = $false
                detail = "Verifier exited with code $exitCode and no output."
            }
        )
    }
} else {
    try {
        $summary = $rawText | ConvertFrom-Json
    } catch {
        $summary = [pscustomobject]@{
            environmentUrl = $EnvironmentUrl
            appId = $AppId
            appUniqueName = $AppUniqueName
            generatedOn = (Get-Date).ToString('o')
            passed = 0
            failed = 1
            checks = @(
                [pscustomobject]@{
                    name = 'verifier-json'
                    passed = $false
                    detail = $rawText
                }
            )
        }
    }
}

$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

$lines = New-Object System.Collections.Generic.List[string]
[void]$lines.Add("# Dylan Deployment Evidence")
[void]$lines.Add("")
[void]$lines.Add("- Generated: $($summary.generatedOn)")
[void]$lines.Add("- Environment: $EnvironmentUrl")
[void]$lines.Add("- App ID: $AppId")
[void]$lines.Add("- App unique name: $AppUniqueName")
[void]$lines.Add("- PAC timeout seconds: $PacTimeoutSeconds")
[void]$lines.Add("- Verifier exit code: $exitCode")
[void]$lines.Add("- Passed checks: $($summary.passed)")
[void]$lines.Add("- Failed checks: $($summary.failed)")
[void]$lines.Add("")
[void]$lines.Add("## Checks")
[void]$lines.Add("")
[void]$lines.Add("| Check | Status | Detail |")
[void]$lines.Add("| --- | --- | --- |")

foreach ($check in @($summary.checks)) {
    $status = if ($check.passed) { 'PASS' } else { 'FAIL' }
    $detail = ($check.detail -replace '\r?\n', ' ' -replace '\|', '/').Trim()
    if ($detail.Length -gt 240) {
        $detail = $detail.Substring(0, 240) + '...'
    }
    [void]$lines.Add("| $($check.name) | $status | $detail |")
}

[void]$lines.Add("")
[void]$lines.Add("## Next Action")
[void]$lines.Add("")
if ([int]$summary.failed -gt 0) {
    [void]$lines.Add("Do not claim Dylan deployment success. Resolve the failed read-only check, then rerun this evidence script.")
} else {
    [void]$lines.Add("Attach this evidence to the deployment handoff before any follow-up live write.")
}

$lines | Set-Content -LiteralPath $markdownPath -Encoding UTF8

[pscustomobject]@{
    ExitCode = $exitCode
    JsonPath = $jsonPath
    MarkdownPath = $markdownPath
    Passed = $summary.passed
    Failed = $summary.failed
}

if ([int]$summary.failed -gt 0 -or $exitCode -ne 0) {
    exit 1
}

# Exercise only local stubs; no PAC authentication or network code is invoked.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$hadExitCode = Test-Path variable:global:LASTEXITCODE
$priorExitCode = if ($hadExitCode) { $global:LASTEXITCODE } else { $null }
$temp = Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString())
New-Item -ItemType Directory -Path $temp | Out-Null
$zip = Join-Path $temp 'fixture.zip'
[IO.File]::WriteAllText($zip, 'Offline fixture; never submitted')
$testCalls = [Collections.Generic.List[object]]::new()
$testState = @{ WhoExit = 0 }
$priorPac = Get-Item function:global:pac -ErrorAction SilentlyContinue
Set-Item function:global:pac -Value ({
    $testCalls.Add(@($args))
    if ($args[0] -eq 'auth' -and $args[1] -eq 'list') {
        $global:LASTEXITCODE = 0
        return '[1] * UNIVERSAL fixture https://fixture.crm6.dynamics.com/ fixture-user Public User'
    }
    if ($args[0] -eq 'org' -and $args[1] -eq 'who') {
        $global:LASTEXITCODE = $testState.WhoExit
        return 'Fixture organization identity'
    }
    if ($args[0] -eq 'solution' -and $args[1] -eq 'import') {
        $global:LASTEXITCODE = 0
        return 'Fixture import result'
    }
    throw 'Unexpected command: offline tests prohibit all authentication/profile commands.'
}.GetNewClosure())
function Assert-Local([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}
try {
    $readiness = & (Join-Path $PSScriptRoot 'Get-EnvironmentReadiness.ps1') -RepoRoot $root -DryRun -Json | ConvertFrom-Json
    Assert-Local ($testCalls.Count -eq 0) 'Readiness dry run touched PAC.'
    Assert-Local ($readiness.NativeAccessVerified -eq $false) 'Local preflight claimed native access.'
    $stored = & (Join-Path $PSScriptRoot 'Get-EnvironmentReadiness.ps1') -RepoRoot $root -Json | ConvertFrom-Json
    Assert-Local ($testCalls.Count -eq 1) 'Stored-profile report did not use one consistent snapshot.'
    Assert-Local ($stored.NativeAccessVerified -eq $false) 'Stored profile was promoted to native access.'
    $testCalls.Clear()
    $importer = Join-Path $PSScriptRoot 'Import-Solution.ps1'
    & $importer -SolutionZip $zip -EnvironmentUrl 'https://fixture.crm6.dynamics.com/' -DryRun | Out-Null
    Assert-Local ($testCalls.Count -eq 0) 'Import preview touched PAC.'
    $failed = $false
    try { & $importer -SolutionZip $zip -EnvironmentUrl '' -DryRun | Out-Null } catch { $failed = $true }
    Assert-Local $failed 'Missing target was accepted.'
    Assert-Local ($testCalls.Count -eq 0) 'Missing target invoked PAC.'
    $testState.WhoExit = 1
    $failed = $false
    try { & $importer -SolutionZip $zip -EnvironmentUrl 'https://fixture.crm6.dynamics.com/' | Out-Null } catch { $failed = $true }
    Assert-Local ($failed -and $testCalls.Count -eq 1) 'Failed read did not prevent import.'
    Assert-Local (($testCalls[0] -join '|') -eq 'org|who|--environment|https://fixture.crm6.dynamics.com/') 'Identity read did not bind the target.'
    $testCalls.Clear(); $testState.WhoExit = 0
    & $importer -SolutionZip $zip -EnvironmentUrl 'https://fixture.crm6.dynamics.com/' | Out-Null
    Assert-Local ($testCalls.Count -eq 2) 'Expected one read and one import only.'
    Assert-Local (($testCalls[1][-2..-1] -join '|') -eq '--environment|https://fixture.crm6.dynamics.com/') 'Import did not bind the target.'
    Write-Output 'Offline preflight, profile isolation, exact-target import and failed-read refusal passed.'
} finally {
    Remove-Item function:global:pac
    if ($priorPac) { Set-Item function:global:pac -Value $priorPac.ScriptBlock }
    if ($hadExitCode) { $global:LASTEXITCODE = $priorExitCode } else { Remove-Variable LASTEXITCODE -Scope Global -ErrorAction SilentlyContinue }
    Remove-Item -LiteralPath $temp -Recurse -Force
}

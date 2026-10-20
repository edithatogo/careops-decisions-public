[CmdletBinding()]
param(
    [string]$EnvironmentUrl = "https://environment.example.invalid/",
    [string]$AppId = "22222222-2222-4222-8222-222222222222",
    [string]$AppUniqueName = "decisions_workspace_64f7b61a",
    [string]$QuickStartPageName = "Quick Start Guide",
    [int]$PacTimeoutSeconds = 60,
    [switch]$CheckMedicationSafetySample,
    [Alias('AsJson')][switch]$Json
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Invoke-PacText {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [int]$TimeoutSeconds = 60
    )

    $outputFile = [System.IO.Path]::GetTempFileName()
    $errorFile = [System.IO.Path]::GetTempFileName()

    try {
        $process = Start-Process -FilePath 'pac' -ArgumentList $Arguments -NoNewWindow -PassThru -RedirectStandardOutput $outputFile -RedirectStandardError $errorFile
        $completed = $process.WaitForExit($TimeoutSeconds * 1000)

        if (-not $completed) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            return [pscustomobject]@{
                ExitCode = 124
                Text = "Timed out after $TimeoutSeconds seconds: pac $($Arguments -join ' ')"
            }
        }

        $text = @(
            if (Test-Path -LiteralPath $outputFile) { Get-Content -LiteralPath $outputFile -Raw }
            if (Test-Path -LiteralPath $errorFile) { Get-Content -LiteralPath $errorFile -Raw }
        ) -join [Environment]::NewLine

        [pscustomobject]@{
            ExitCode = $process.ExitCode
            Text = $text
        }
    } finally {
        Remove-Item -LiteralPath $outputFile -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $errorFile -Force -ErrorAction SilentlyContinue
    }
}

function New-CheckResult {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][bool]$Passed,
        [string]$Detail = ''
    )

    [pscustomobject]@{
        name = $Name
        passed = $Passed
        detail = $Detail.Trim()
    }
}

if (-not (Get-Command pac -ErrorAction SilentlyContinue)) {
    throw "Power Platform CLI 'pac' was not found on PATH."
}

$checks = New-Object System.Collections.Generic.List[object]

$who = Invoke-PacText -Arguments @('env', 'who', '--environment', $EnvironmentUrl) -TimeoutSeconds $PacTimeoutSeconds
$checks.Add((New-CheckResult -Name 'connect-dylan' -Passed ($who.ExitCode -eq 0 -and $who.Text -match 'Dylan\.Mordaunt@health\.nsw\.gov\.au') -Detail $who.Text))

if (-not $checks[0].passed) {
    $summary = [pscustomobject]@{
        environmentUrl = $EnvironmentUrl
        appId = $AppId
        appUniqueName = $AppUniqueName
        generatedOn = (Get-Date).ToString('o')
        passed = 0
        failed = 1
        checks = @($checks.ToArray())
    }

    if ($Json) {
        $summary | ConvertTo-Json -Depth 8
    } else {
        Write-Host "Dylan deployment verification"
        Write-Host "Environment: $EnvironmentUrl"
        Write-Host "App ID: $AppId"
        Write-Host "FAIL connect-dylan"
        Write-Host $who.Text
        Write-Host "Passed: 0"
        Write-Host "Failed: 1"
    }

    exit 1
}

$appFetchXml = "<fetch count='5'><entity name='appmodule'><attribute name='uniquename'/><attribute name='name'/><attribute name='appmoduleid'/><filter><condition attribute='uniquename' operator='eq' value='$AppUniqueName'/></filter></entity></fetch>"
$app = Invoke-PacText -Arguments @('env', 'fetch', '--environment', $EnvironmentUrl, '--xml', $appFetchXml) -TimeoutSeconds $PacTimeoutSeconds
$checks.Add((New-CheckResult -Name 'app-module' -Passed ($app.ExitCode -eq 0 -and $app.Text -match [regex]::Escape($AppUniqueName) -and $app.Text -match [regex]::Escape($AppId)) -Detail $app.Text))

$pages = Invoke-PacText -Arguments @('model', 'genpage', 'list', '--environment', $EnvironmentUrl, '--app-id', $AppId) -TimeoutSeconds $PacTimeoutSeconds
$checks.Add((New-CheckResult -Name 'quick-start-generated-page' -Passed ($pages.ExitCode -eq 0 -and $pages.Text -match [regex]::Escape($QuickStartPageName)) -Detail $pages.Text))

$requiredTables = @(
    'dft_meeting',
    'dft_agendaitem',
    'dft_paper',
    'dft_minutes',
    'dft_decision',
    'dft_actionitem',
    'dft_governancebody',
    'dft_meetingseries',
    'dft_termsofreference',
    'dft_committeemembership',
    'dft_meetingfeatureoverride'
)

foreach ($table in $requiredTables) {
    $fetchXml = "<fetch count='1'><entity name='$table'><attribute name='createdon'/></entity></fetch>"
    $tableCheck = Invoke-PacText -Arguments @('env', 'fetch', '--environment', $EnvironmentUrl, '--xml', $fetchXml) -TimeoutSeconds $PacTimeoutSeconds
    $checks.Add((New-CheckResult -Name "table-$table" -Passed ($tableCheck.ExitCode -eq 0) -Detail $tableCheck.Text))
}

if ($CheckMedicationSafetySample) {
    $meetingFetchXml = "<fetch count='5'><entity name='dft_meeting'><attribute name='dft_name'/><attribute name='dft_externalcalendarid'/><filter><condition attribute='dft_externalcalendarid' operator='eq' value='calendar-medication-safety-2026-05-20-placeholder'/></filter></entity></fetch>"
    $meeting = Invoke-PacText -Arguments @('env', 'fetch', '--environment', $EnvironmentUrl, '--xml', $meetingFetchXml) -TimeoutSeconds $PacTimeoutSeconds
    $checks.Add((New-CheckResult -Name 'medication-safety-sample-meeting' -Passed ($meeting.ExitCode -eq 0 -and $meeting.Text -notmatch 'No results returned') -Detail $meeting.Text))
}

$passed = @($checks | Where-Object { $_.passed }).Count
$failed = @($checks | Where-Object { -not $_.passed }).Count

$summary = [pscustomobject]@{
    environmentUrl = $EnvironmentUrl
    appId = $AppId
    appUniqueName = $AppUniqueName
    generatedOn = (Get-Date).ToString('o')
    passed = $passed
    failed = $failed
    checks = @($checks.ToArray())
}

if ($Json) {
    $summary | ConvertTo-Json -Depth 8
} else {
    Write-Host "Dylan deployment verification"
    Write-Host "Environment: $EnvironmentUrl"
    Write-Host "App ID: $AppId"
    foreach ($check in $checks) {
        $status = if ($check.passed) { 'PASS' } else { 'FAIL' }
        Write-Host "$status $($check.name)"
        if (-not $check.passed -and -not [string]::IsNullOrWhiteSpace($check.detail)) {
            Write-Host $check.detail
        }
    }
    Write-Host "Passed: $passed"
    Write-Host "Failed: $failed"
}

if ($failed -gt 0) {
    exit 1
}

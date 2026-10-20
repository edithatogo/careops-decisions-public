param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [string]$SamplePath = 'powerplatform/settings/deployment-settings.sample.json',
    [string]$OutputPath = 'powerplatform/settings/deployment-settings.local.json',
    [string]$TargetProfileName = 'development',
    [string]$EnvironmentUrl = $env:POWER_PLATFORM_ENVIRONMENT_URL,
    [string]$TeamsTeamId,
    [string]$TeamsChannelId,
    [string]$PlannerPlanId,
    [string]$SharePointSiteUrl,
    [string]$MinutesLibraryName,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot 'Resolve-AlmProfile.ps1')

function Resolve-RepoPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path $RepoRoot $Path
}

function Get-ConnectionRows {
    param([Parameter(Mandatory = $true)][string]$TargetEnvironmentUrl)

    $output = & pac connection list --environment $TargetEnvironmentUrl 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "pac connection list failed: $(($output | Out-String).Trim())"
    }

    $rows = New-Object System.Collections.Generic.List[object]
    foreach ($line in $output) {
        $text = [string]$line
        if ($text -notmatch '^(\S+)\s+(.+?)\s+(/providers/Microsoft\.PowerApps/apis/\S+)\s+(\S+)\s*$') {
            continue
        }

        $rows.Add([pscustomobject]@{
            Id = $Matches[1]
            Name = $Matches[2].Trim()
            ApiId = $Matches[3].Trim()
            Status = $Matches[4].Trim()
        })
    }

    return @($rows.ToArray())
}

function Select-ConnectionForApi {
    param(
        [Parameter(Mandatory = $true)][object[]]$Connections,
        [Parameter(Mandatory = $true)][string]$ApiName,
        [string]$PreferredName
    )

    $apiId = "/providers/Microsoft.PowerApps/apis/$ApiName"
    $matches = @($Connections | Where-Object { $_.ApiId -eq $apiId -and $_.Status -eq 'Connected' })
    if ($PreferredName) {
        $preferred = @($matches | Where-Object { $_.Name -eq $PreferredName } | Select-Object -First 1)
        if ($preferred) {
            return $preferred
        }
    }

    return @($matches | Select-Object -First 1)
}

function Set-EnvironmentVariableValue {
    param(
        [object]$Settings,
        [string]$SchemaName,
        [string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return
    }

    foreach ($entry in @($Settings.EnvironmentVariables)) {
        if ($entry.SchemaName -eq $SchemaName) {
            $entry.Value = $Value
            return
        }
    }
}

$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
$sampleFullPath = Resolve-RepoPath -Path $SamplePath
$outputFullPath = Resolve-RepoPath -Path $OutputPath

if (-not (Test-Path -LiteralPath $sampleFullPath)) {
    throw "Sample deployment settings not found: $sampleFullPath"
}

$profile = (Resolve-AlmProfile -RepoRoot $RepoRoot -ProfileName $TargetProfileName).Profile
$targetEnvironmentUrl = Resolve-AlmTargetEnvironmentUrl -Profile $profile -EnvironmentUrl $EnvironmentUrl
Select-PacAuthEnvironment -EnvironmentUrl $targetEnvironmentUrl | Out-Null

$settings = Get-Content -LiteralPath $sampleFullPath -Raw | ConvertFrom-Json
$connections = Get-ConnectionRows -TargetEnvironmentUrl $targetEnvironmentUrl

$connectionMap = @{
    decisions_sharedcommondataserviceforapps = 'shared_commondataserviceforapps'
    decisions_sharedteams = 'shared_teams'
    decisions_sharedplanner = 'shared_planner'
    decisions_sharedsharepointonline = 'shared_sharepointonline'
    decisions_sharedoffice365users = 'shared_office365users'
    decisions_sharedoffice365 = 'shared_office365'
    decisions_sharedaibuilder = 'shared_commondataserviceforapps'
}

$bindingResults = New-Object System.Collections.Generic.List[object]
foreach ($reference in @($settings.ConnectionReferences)) {
    $apiName = $connectionMap[$reference.LogicalName]
    if (-not $apiName) {
        continue
    }

    $connection = Select-ConnectionForApi -Connections $connections -ApiName $apiName -PreferredName 'operator@example.invalid'
    if ($connection) {
        $reference.ConnectorId = "/providers/Microsoft.PowerApps/apis/$apiName"
        $reference.ConnectionId = "$($reference.ConnectorId)/connections/$($connection.Id)"
        $bindingResults.Add([pscustomobject]@{
            LogicalName = $reference.LogicalName
            ApiName = $apiName
            Status = 'Bound'
            ConnectionId = $connection.Id
        })
    } else {
        $bindingResults.Add([pscustomobject]@{
            LogicalName = $reference.LogicalName
            ApiName = $apiName
            Status = 'Missing'
            ConnectionId = $null
        })
    }
}

Set-EnvironmentVariableValue -Settings $settings -SchemaName 'decisions_TeamsTeamId' -Value $TeamsTeamId
Set-EnvironmentVariableValue -Settings $settings -SchemaName 'decisions_TeamsChannelId' -Value $TeamsChannelId
Set-EnvironmentVariableValue -Settings $settings -SchemaName 'decisions_PlannerPlanId' -Value $PlannerPlanId
Set-EnvironmentVariableValue -Settings $settings -SchemaName 'decisions_SharePointSiteUrl' -Value $SharePointSiteUrl
Set-EnvironmentVariableValue -Settings $settings -SchemaName 'decisions_MinutesLibraryName' -Value $MinutesLibraryName

$json = $settings | ConvertTo-Json -Depth 20
if (-not $DryRun) {
    $outputDirectory = Split-Path -Parent $outputFullPath
    if ($outputDirectory -and -not (Test-Path -LiteralPath $outputDirectory)) {
        New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    }

    Set-Content -LiteralPath $outputFullPath -Value $json -Encoding utf8
}

[pscustomobject]@{
    OutputPath = $outputFullPath
    TargetProfileName = $TargetProfileName
    TargetEnvironmentUrl = $targetEnvironmentUrl
    DryRun = [bool]$DryRun
    BoundConnectionReferences = @($bindingResults | Where-Object { $_.Status -eq 'Bound' }).Count
    MissingConnectionReferences = @($bindingResults | Where-Object { $_.Status -eq 'Missing' } | ForEach-Object { $_.LogicalName })
    PlaceholderEnvironmentVariables = @($settings.EnvironmentVariables | Where-Object { Test-AlmPlaceholderValue -Value ([string]$_.Value) } | ForEach-Object { $_.SchemaName })
    PlaceholderConnectionReferences = @($settings.ConnectionReferences | Where-Object { Test-AlmPlaceholderValue -Value ([string]$_.ConnectionId) } | ForEach-Object { $_.LogicalName })
    BindingResults = @($bindingResults.ToArray())
}

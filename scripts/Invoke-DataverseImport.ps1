[CmdletBinding()]
param(
    [string]$PayloadPath = "artifacts/seed/sample-governance-dataverse-payloads.json",
    [string]$EnvironmentUrl = "https://environment.example.invalid/",
    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$payloadFullPath = Join-Path $repoRoot $PayloadPath
if (-not (Test-Path -LiteralPath $payloadFullPath)) { throw "Payload file not found: $payloadFullPath" }

$token = az account get-access-token --resource $EnvironmentUrl --query accessToken -o tsv
$headers = @{Authorization = "Bearer $token"; "Content-Type" = "application/json"; Accept = "application/json"}
$base = "$EnvironmentUrl/api/data/v9.2"

$payloadBundle = Get-Content -LiteralPath $payloadFullPath -Raw | ConvertFrom-Json

$created = 0
$skipped = 0
$errors = 0
$guidCache = @{}

# Map logical table names to correct Dataverse entity set names
$tableEntityMap = @{
    "dft_governancebody" = "dft_governancebodies"
    "dft_termsofreference" = "dft_termsofreferences"
    "dft_orgperson" = "dft_orgpersons"
    "dft_committeemembership" = "dft_committeememberships"
    "dft_orgrelationship" = "dft_orgrelationships"
    "dft_featuredefinition" = "dft_featuredefinitions"
    "dft_meetingseriesfeature" = "dft_featuretemplates"
    "dft_meetingseries" = "dft_meetingseries"
    "dft_meeting" = "dft_meetings"
    "dft_meetingfeatureoverride" = "dft_meetingfeatureoverrides"
    "dft_agendaitem" = "dft_agendaitems"
    "dft_paper" = "dft_papers"
    "dft_minutes" = "dft_minuteses"
    "dft_decision" = "dft_decisions"
    "dft_actionitem" = "dft_actionitems"
    "dft_accessrule" = "dft_accessrules"
}

function Get-EntitySet {
    param([string]$TableName)
    if ($tableEntityMap.ContainsKey($TableName)) { return $tableEntityMap[$TableName] }
    return $TableName
}

function Get-NaturalKeyFilter {
    param([string]$Table, [string]$Field, [string]$Value)
    if (-not $Value -or $Value -eq 'null') { return $null }
    # Escape single quotes
    $escaped = $Value -replace "'", "''"
    # Determine the alternate key field
    $keyMap = @{
        "dft_governancebody" = "dft_bodycode"
        "dft_orgperson" = "dft_workemail"
        "dft_featuredefinition" = "dft_featurecode"
        "dft_meetingseries" = "dft_seriescode"
        "dft_meeting" = "dft_externalcalendarid"
        "dft_agendaitem" = "dft_name"
        "dft_decision" = "dft_decisionnumber"
        "dft_actionitem" = "dft_actionnumber"
        "dft_paper" = "dft_documentid"
    }
    $kf = if ($keyMap.ContainsKey($Table)) { $keyMap[$Table] } else { $Field -replace '^dft_(\w+)id$', 'dft_name' }
    # If already cached, use the GUID
    if ($guidCache.ContainsKey("$Table|$Value")) { return $guidCache["$Table|$Value"] }
    return @{field = $kf; value = $Value; table = $Table}
}

function Resolve-Lookup {
    param($Binding)
    if (-not $Binding -or -not $Binding.targetNaturalKey -or $Binding.targetNaturalKey -eq 'null') { return $null }
    $cacheKey = "$($Binding.targetTable)|$($Binding.targetNaturalKey)"
    if ($guidCache.ContainsKey($cacheKey)) {
        $guid = $guidCache[$cacheKey]
        return "/$($Binding.targetTable)($guid)"
    }
    $keyField = $Binding.targetNaturalKeyField
    $keyValue = $Binding.targetNaturalKey
    $filter = [System.Web.HttpUtility]::UrlEncode("$keyField eq '$keyValue'")
    try {
        $result = Invoke-RestMethod -Uri "$base/$($Binding.targetTable)?`$top=1&`$select=$keyField&`$filter=$filter" -Headers $headers -ErrorAction Stop
        if ($result.value -and $result.value.Count -gt 0) {
            $id = $result.value[0].PSObject.Properties | Where-Object { $_.Name -match '^[a-f0-9-]{36}$' -or $_.Name -eq "activityid" -or $_.Name.EndsWith("id") -and $_.Name -notlike "dft_*" } | Select-Object -First 1
            if (-not $id) { $id = $result.value[0].PSObject.Properties | Select-Object -First 1 }
            if ($id) {
                $guidCache[$cacheKey] = $id.Value
                return "/$($Binding.targetTable)($($id.Value))"
            }
        }
    } catch { return $null }
    return $null
}

Write-Host "Importing $($payloadBundle.payloads.Count) records..." -ForegroundColor Cyan
if ($WhatIf) { Write-Host "WhatIf mode - no writes"; return }

foreach ($pl in $payloadBundle.payloads) {
    $body = @{}
    foreach ($prop in $pl.payload.PSObject.Properties) {
        if ($null -ne $prop.Value -and [string]$prop.Value -ne 'null') {
            $body[$prop.Name] = $prop.Value
        }
    }
    foreach ($lb in $pl.lookupBindings.PSObject.Properties) {
        $binding = $lb.Value
        if ($binding.targetTable -and $binding.targetNaturalKey) {
            $resolved = Resolve-Lookup $binding
            if ($resolved) { $body["$($lb.Name)@odata.bind"] = $resolved }
            else { Write-Warning "  Cannot resolve $($binding.targetTable):$($binding.targetNaturalKey)"; $skipped++; continue }
        }
    }

    try {
        $jsonBody = $body | ConvertTo-Json -Depth 5 -Compress
        $response = Invoke-RestMethod -Uri "$base/$(Get-EntitySet $pl.table)" -Method Post -Headers $headers -Body $jsonBody -ErrorAction Stop
        Write-Host "  CREATED $($pl.table):$($pl.naturalKey)" -ForegroundColor Green
        $created++
    } catch {
        $code = $_.Exception.Response.StatusCode.value__
        if ($code -eq 204) {
            Write-Host "  CREATED $($pl.table):$($pl.naturalKey) (204)" -ForegroundColor Green
            $created++
        } else {
            Write-Host "  ERROR $($pl.table):$($pl.naturalKey) - $code" -ForegroundColor Yellow
            $errors++
        }
    }
}

Write-Host "`nImport: $created created, $skipped skipped, $errors errors" -ForegroundColor Cyan

[CmdletBinding()]
param(
    [string]$OutputPath = "artifacts/seed/validation-sample-governance-import-plan.json",
    [string]$PayloadPath = "artifacts/seed/validation-sample-governance-dataverse-payloads.json",
    [switch]$DryRun,
    [switch]$PassThru
)

$ErrorActionPreference = 'Stop'
$failures = New-Object System.Collections.Generic.List[string]
$warnings = New-Object System.Collections.Generic.List[string]

$repoRoot = Split-Path -Parent $PSScriptRoot

function Resolve-RepoPath {
    param([Parameter(Mandatory = $true)][string]$PathValue)
    if ([System.IO.Path]::IsPathRooted($PathValue)) { return $PathValue }
    return Join-Path $repoRoot $PathValue
}

function Add-Failure {
    param([string]$Message)
    $failures.Add($Message)
    Write-Verbose "FAIL: $Message"
}

function Add-Warning {
    param([string]$Message)
    $warnings.Add($Message)
    Write-Verbose "WARN: $Message"
}

function Assert-HasProperty {
    param(
        [Parameter(Mandatory = $true)][object]$Object,
        [Parameter(Mandatory = $true)][string]$PropertyName,
        [Parameter(Mandatory = $true)][string]$Context
    )
    if (-not $Object.PSObject.Properties[$PropertyName]) {
        Add-Failure "$Context is missing required property '$PropertyName'."
    }
}

function Assert-RequiredTextProperty {
    param(
        [Parameter(Mandatory = $true)][object]$Object,
        [Parameter(Mandatory = $true)][string]$PropertyName,
        [Parameter(Mandatory = $true)][string]$Context
    )
    Assert-HasProperty -Object $Object -PropertyName $PropertyName -Context $Context
    $value = $Object.PSObject.Properties[$PropertyName].Value
    if ([string]::IsNullOrWhiteSpace([string]$value)) {
        Add-Failure "$Context has empty required property '$PropertyName'."
    }
}

function Assert-NoNullFields {
    param(
        [Parameter(Mandatory = $true)][object]$Object,
        [Parameter(Mandatory = $true)][string]$Context
    )
    foreach ($property in $Object.PSObject.Properties) {
        if ($null -eq $property.Value) {
            Add-Failure "$Context contains null-valued field '$($property.Name)'."
        }
    }
}

function Assert-NaturalKeyFormat {
    param(
        [Parameter(Mandatory = $true)][string]$NaturalKey,
        [Parameter(Mandatory = $true)][string]$SourceCollection,
        [Parameter(Mandatory = $true)][string]$Context
    )

    $patterns = @{
        governanceBodies          = '^[a-z0-9_-]+$'
        committees                = '^[a-z0-9_-]+$'
        termsOfReference          = '^[a-z0-9_-]+:[a-zA-Z0-9.]+$'
        members                   = '^([a-z0-9_-]+:)?[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$'
        orgRelationships          = '^[a-z0-9_-]+:[a-z0-9_-]+:[a-z_]+$'
        featureDefinitions        = '^[a-z_]+$'
        meetingSeriesFeatureDefaults = '^[a-z0-9_-]+:[a-z0-9_-]+$'
        meetingFeatureOverrides   = '^[a-z0-9_-]+:[a-z0-9_-]+$'
        meetingSeries             = '^[a-z0-9_-]+$'
        meetings                  = '.+'
        attendees                 = '^[a-z0-9_-]+:.+'
        agendaItems               = '^[a-z0-9_-]+:[0-9]+$'
        papers                    = '^[a-z0-9_-]+$'
        minutes                   = '^[a-z0-9_-]+:[0-9]+$'
        transcripts               = '^[a-z0-9_-]+$'
        aiBuilderPrompts          = '^[a-z0-9_-]+:[0-9.]+$'
        aiBuilderRuns             = '^[a-z0-9_-]+$'
        decisions                 = '^[A-Z]+-[0-9]+$'
        actions                   = '^[A-Z]+-[0-9]+$'
        accessRules               = '^[a-z_]+:[a-z_]+:[a-z_]+$'
    }

    if ($DryRun -and $patterns.ContainsKey($SourceCollection)) {
        $expected = $patterns[$SourceCollection]
        if ($NaturalKey -notmatch $expected) {
            Add-Warning "$context naturalKey '$NaturalKey' (collection: $SourceCollection) does not match expected pattern: $expected"
        }
    }
}

function Assert-PayloadFieldsHaveDftPrefix {
    param(
        [Parameter(Mandatory = $true)][object]$Payload,
        [Parameter(Mandatory = $true)][string]$Context
    )
    foreach ($property in $Payload.PSObject.Properties) {
        if ($property.Name -notlike 'dft_*') {
            Add-Warning "$context has non-standard field name '$($property.Name)' (expected 'dft_' prefix)"
        }
    }
}

$importScript = Join-Path $repoRoot 'scripts/Import-SeedData.ps1'
$planFullPath = Resolve-RepoPath $OutputPath
$payloadFullPath = Resolve-RepoPath $PayloadPath

if (-not (Test-Path -LiteralPath $importScript)) {
    Add-Failure "Seed import script not found: $importScript"
} else {
    & $importScript -WritePayloads -OutputPath $planFullPath -PayloadPath $payloadFullPath | Out-Null

    if (-not (Test-Path -LiteralPath $planFullPath)) {
        Add-Failure "Seed import plan was not generated: $planFullPath"
    }

    if (-not (Test-Path -LiteralPath $payloadFullPath)) {
        Add-Failure "Dataverse payload bundle was not generated: $payloadFullPath"
    }

    if ($failures.Count -eq 0) {
        $plan = Get-Content -LiteralPath $planFullPath -Raw | ConvertFrom-Json -ErrorAction Stop
        $payloadBundle = Get-Content -LiteralPath $payloadFullPath -Raw | ConvertFrom-Json -ErrorAction Stop
        $planSteps = @($plan.steps)
        $payloads = @($payloadBundle.payloads)

        $knownModes = @('dataverse-upsert-payload')

        if ($plan.stepCount -ne $planSteps.Count) {
            Add-Failure "Plan stepCount '$($plan.stepCount)' does not match steps count '$($planSteps.Count)'."
        }

        if ($payloadBundle.payloadCount -ne $payloads.Count) {
            Add-Failure "Payload bundle payloadCount '$($payloadBundle.payloadCount)' does not match payloads count '$($payloads.Count)'."
        }

        if ($plan.stepCount -ne $payloadBundle.payloadCount) {
            Add-Failure "Plan stepCount '$($plan.stepCount)' does not match payload count '$($payloadBundle.payloadCount)'."
        }

        if ($plan.generatedOn -notmatch '^\d{4}-\d{2}-\d{2}T') {
            Add-Warning "Plan generatedOn '$($plan.generatedOn)' is not in ISO 8601 format."
        }

        for ($index = 0; $index -lt $payloads.Count; $index++) {
            $payloadEntry = $payloads[$index]
            $context = "Payload[$index]"

            Assert-RequiredTextProperty -Object $payloadEntry -PropertyName 'table' -Context $context
            Assert-RequiredTextProperty -Object $payloadEntry -PropertyName 'naturalKey' -Context $context
            Assert-HasProperty -Object $payloadEntry -PropertyName 'payload' -Context $context
            Assert-RequiredTextProperty -Object $payloadEntry -PropertyName 'mode' -Context $context

            $mode = $payloadEntry.mode
            if ($mode -notin $knownModes) {
                Add-Failure "$context has unknown mode '$mode'. Known modes: $($knownModes -join ', ')"
            }

            if ($null -eq $payloadEntry.payload) {
                Add-Failure "$context has null payload."
            } else {
                Assert-NoNullFields -Object $payloadEntry.payload -Context "$context.payload"
                Assert-PayloadFieldsHaveDftPrefix -Payload $payloadEntry.payload -Context "$context.payload"

                if ($payloadEntry.sourceCollection) {
                    Assert-NaturalKeyFormat -NaturalKey $payloadEntry.naturalKey -SourceCollection $payloadEntry.sourceCollection -Context $context
                }
            }

            if ($payloadEntry.PSObject.Properties['lookupBindings'] -and $null -ne $payloadEntry.lookupBindings) {
                foreach ($bindingProperty in $payloadEntry.lookupBindings.PSObject.Properties) {
                    $bindingContext = "$context.lookupBindings.$($bindingProperty.Name)"
                    $binding = $bindingProperty.Value
                    Assert-RequiredTextProperty -Object $binding -PropertyName 'targetTable' -Context $bindingContext
                    Assert-RequiredTextProperty -Object $binding -PropertyName 'targetNaturalKeyField' -Context $bindingContext
                    Assert-RequiredTextProperty -Object $binding -PropertyName 'targetNaturalKey' -Context $bindingContext

                    if ($binding.targetTable -notlike 'dft_*') {
                        Add-Warning "$bindingContext targetTable '$($binding.targetTable)' does not use expected 'dft_' prefix."
                    }
                }
            }
        }

        if ($plan.counts) {
            foreach ($key in $plan.counts.PSObject.Properties.Name) {
                $val = $plan.counts.$key
                if ($val -lt 1) {
                    Add-Warning "Collection '$key' has zero records in seed data."
                }
            }
        }
    }
}

if ($PassThru) {
    [pscustomobject]@{
        Passed    = ($failures.Count -eq 0)
        Failures  = @($failures)
        Warnings  = @($warnings)
        Summary   = "Validated seed import plan with $($failures.Count) failures and $($warnings.Count) warnings."
    }
    return
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

if ($warnings.Count -gt 0) {
    $warnings | ForEach-Object { Write-Warning $_ }
}

Write-Host "Seed payload validation passed. ($($failures.Count) failures, $($warnings.Count) warnings)"

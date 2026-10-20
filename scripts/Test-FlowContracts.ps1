[CmdletBinding()]
param(
    [switch]$DryRun,
    [switch]$PassThru
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$flowsRoot = Join-Path $repoRoot 'powerplatform/src/flows'
$catalogPath = Join-Path $flowsRoot 'flow-catalog.yaml'
$implementationChecklistPath = Join-Path $flowsRoot 'implementation-checklist.json'
$failures = New-Object System.Collections.Generic.List[string]
$warnings = New-Object System.Collections.Generic.List[string]

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

function Get-RelativePathText {
    param(
        [Parameter(Mandatory = $true)][string]$BasePath,
        [Parameter(Mandatory = $true)][string]$TargetPath
    )
    $baseFull = [System.IO.Path]::GetFullPath($BasePath).TrimEnd('\', '/')
    $targetFull = [System.IO.Path]::GetFullPath($TargetPath)
    if ($targetFull.StartsWith($baseFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        $relative = $targetFull.Substring($baseFull.Length).TrimStart('\', '/')
        if ([string]::IsNullOrWhiteSpace($relative)) { return '.' }
        return $relative
    }
    return $TargetPath
}

function Get-FlowCatalogEntries {
    param([Parameter(Mandatory = $true)][string]$Path)
    $entries = @()
    $current = $null
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ($line -match '^\s{2}-\s+name:\s*(.+?)\s*$') {
            if ($null -ne $current) { $entries += [pscustomobject]$current }
            $current = [ordered]@{ Name = $Matches[1].Trim(); File = $null; Connectors = @(); EnvVars = @() }
            continue
        }
        if ($null -ne $current -and $line -match '^\s{4}file:\s*(.+?)\s*$') {
            $current.File = $Matches[1].Trim()
        }
        if ($null -ne $current -and $line -match '^\s{8}-\s*(.+?)\s*$' -and $line -notmatch '^\s{8}-\s+name:') {
            if ($current.Connectors.Count -gt 0 -or ($current.PSObject.Properties['inConnectorsSection'] -and $current.inConnectorsSection)) {
                $current.Connectors += $Matches[1].Trim()
            }
        }
        if ($null -ne $current -and $line -match '^\s{6}-\s+Dataverse\s*$') {
            $current | Add-Member -NotePropertyName 'inConnectorsSection' -NotePropertyValue $true -Force
            $current.Connectors += 'Dataverse'
        }
        if ($null -ne $current -and $line -match '^\s{6}-\s+Planner\s*$') {
            $current.Connectors += 'Planner'
        }
        if ($null -ne $current -and $line -match '^\s{6}-\s+Microsoft\s*Teams\s*$') {
            $current.Connectors += 'Microsoft Teams'
        }
        if ($null -ne $current -and $line -match '^\s{6}-\s+Office\s*365\s*Outlook\s*$') {
            $current.Connectors += 'Office 365 Outlook'
        }
        if ($null -ne $current -and $line -match '^\s{6}-\s+Office\s*365\s*Users\s*$') {
            $current.Connectors += 'Office 365 Users'
        }
        if ($null -ne $current -and $line -match '^\s{6}-\s+AI\s*Builder\s*$') {
            $current.Connectors += 'AI Builder'
        }
        if ($null -ne $current -and $line -match '^\s{8}-\s*(.+?)\s*$' -and $null -ne $current.Connectors -and $current.Connectors.Count -gt 0) {
        }
    }
    if ($null -ne $current) { $entries += [pscustomobject]$current }
    return @($entries)
}

function Test-RequiredPattern {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$Pattern,
        [Parameter(Mandatory = $true)][string]$FailureMessage
    )
    if ($Content -notmatch $Pattern) { Add-Failure $FailureMessage }
}

function Test-RequiredWarningPattern {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$Pattern,
        [Parameter(Mandatory = $true)][string]$WarningMessage
    )
    if ($Content -notmatch $Pattern) { Add-Warning $WarningMessage }
}

function Test-FlowSpec {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $relative = Get-RelativePathText -BasePath $repoRoot -TargetPath $Path
    $content = Get-Content -LiteralPath $Path -Raw

    foreach ($pattern in @(
        '(?m)^schema:\s*decisions\.workspace\.flow\.v1\s*$',
        '(?m)^flow:\s*$',
        '(?m)^\s{2}name:\s*',
        '(?m)^\s{2}trigger:\s*$',
        '(?m)^\s{2}inputs:\s*$',
        '(?m)^\s{4}readinessChecks:\s*$',
        '(?m)^\s{2}outputs:\s*$',
        '(?m)^\s{2}idempotency:\s*$',
        '(?m)^\s{2}connectorCalls:\s*$',
        '(?m)^\s{2}steps:\s*$',
        '(?m)^testHooks:\s*$'
    )) {
        Test-RequiredPattern -Content $content -Pattern $pattern -FailureMessage "Missing required flow contract section '$pattern' in $relative"
    }

    Test-RequiredPattern -Content $content -Pattern "(?m)^\s{2}name:\s*$([regex]::Escape($Name))\s*$" -FailureMessage "Catalog name '$Name' does not match flow name in $relative"
    Test-RequiredPattern -Content $content -Pattern '(?m)^\s+-\s+connector:\s+Dataverse\s*$' -FailureMessage "Flow does not declare a Dataverse connector call in $relative"
    Test-RequiredPattern -Content $content -Pattern 'dft_IntegrationLog' -FailureMessage "Flow does not declare integration logging to dft_IntegrationLog in $relative"
    Test-RequiredPattern -Content $content -Pattern '(?m)^\s+kind:\s+dataverse\.(createRow|upsertRow|updateRow|getRow|listRows)\s*$' -FailureMessage "Flow does not show Dataverse-first implementation steps in $relative"
    Test-RequiredPattern -Content $content -Pattern '(?im)(skip|skipped|idempot|duplicate|replay)' -FailureMessage "Flow does not declare replay/idempotency behavior in $relative"

    if ($DryRun) {
        $dataverseStepCount = [regex]::Matches($content, '(?m)^\s+kind:\s+dataverse\.(createRow|upsertRow|updateRow|getRow|listRows)\s*$').Count
        if ($dataverseStepCount -lt 2) {
            Add-Warning "Flow $relative has only $dataverseStepCount Dataverse steps (expected at least 2 for meaningful integration logging)."
        }

        Test-RequiredWarningPattern -Content $content -Pattern '(?m)^\s{4}sourceFields:\s*$' -WarningMessage "Flow $relative is missing explicit sourceFields section in inputs."

        $envVarCount = [regex]::Matches($content, "decisions_\w+").Count
        if ($envVarCount -lt 1) {
            Add-Warning "Flow $relative does not reference any environment variables."
        }

        $testHookCount = [regex]::Matches($content, '(?m)^\s{2}-\s+name:\s+').Count
        if ($testHookCount -lt 1) {
            Add-Warning "Flow $relative has no test hook entries."
        }

        $connectorCallCount = [regex]::Matches($content, '(?m)^\s+-\s+connector:\s+').Count
        if ($connectorCallCount -lt 2) {
            Add-Warning "Flow $relative has only $connectorCallCount connector calls (expected at least 2 including Dataverse)."
        }

        if ($content -match '(?i)(SharePoint|Graph)\s') {
            Test-RequiredWarningPattern -Content $content -Pattern '(?im)(deferred|future|not yet|environment.gated|placeholder|skip)' -WarningMessage "Flow $relative references SharePoint or Graph but may not mark it as deferred/environment-gated."
        }
    }

    $externalConnectors = @(
        [pscustomobject]@{ Connector = 'Planner'; Gate = '(decisions_PlannerPlanId|Planner connector is available|Planner connector calls run)' },
        [pscustomobject]@{ Connector = 'Microsoft Teams'; Gate = '(decisions_TeamsTeamId|decisions_TeamsChannelId|Teams connector calls run)' },
        [pscustomobject]@{ Connector = 'Office 365 Outlook'; Gate = '(Outlook connector is available|create or update calls run)' },
        [pscustomobject]@{ Connector = 'Office 365 Users'; Gate = '(Office 365 Users connector is available|profile and manager lookups run)' },
        [pscustomobject]@{ Connector = 'AI Builder'; Gate = '(decisions_EnableAiBuilderMinutes|decisions_AiBuilderPromptName|AI Builder connector calls run)' }
    )

    foreach ($external in $externalConnectors) {
        if ($content -match "(?m)^\s+-\s+connector:\s+$([regex]::Escape($external.Connector))\s*$") {
            Test-RequiredPattern -Content $content -Pattern '(?m)^\s{4}readinessChecks:\s*$' -FailureMessage "External connector flow is missing readiness checks in $relative"
            Test-RequiredPattern -Content $content -Pattern $external.Gate -FailureMessage "External connector '$($external.Connector)' is not deployment-gated in $relative"
            Test-RequiredPattern -Content $content -Pattern '(?im)(configured|available|feature flag|enable|before .*connector calls run|before create or update calls run|before profile and manager lookups run)' -FailureMessage "External connector readiness gate is not explicit in $relative"
        }
    }

    if ($Name -eq 'ai-transcript-minutes') {
        Test-RequiredPattern -Content $content -Pattern '(?im)human[- ]review|human review' -FailureMessage "AI transcript minutes flow is missing human review guardrail wording."
        Test-RequiredPattern -Content $content -Pattern '(?im)never publish|not published|not publishable|draft only' -FailureMessage "AI transcript minutes flow is missing never-publish-direct wording."
        Test-RequiredPattern -Content $content -Pattern '(?m)^\s{2}reviewGate:\s*$' -FailureMessage "AI transcript minutes flow is missing a reviewGate contract."
        Test-RequiredPattern -Content $content -Pattern '(?im)draft records|draft minutes|Draft' -FailureMessage "AI transcript minutes flow does not constrain output to draft records."
    }

    if ($Name -in @('planner-sync', 'teams-summary-post')) {
        Test-RequiredPattern -Content $content -Pattern '(?m)^\s{4}bindingCandidates:\s*$' -FailureMessage "DMS-connected flow is missing bindingCandidates in $relative"
        Test-RequiredPattern -Content $content -Pattern '(?im)optional local dev-space binding' -FailureMessage "DMS candidate values must be optional local binding candidates in $relative"

        $requiredDmsPattern = '(?im)(DMS|Workplan|Executive Team|ExecTeam)[\s\S]{0,160}(required|production|required production)'
        if ($content -match $requiredDmsPattern) {
            Add-Failure "DMS candidate values appear to be required production values in $relative"
        }
    }
}

if (-not (Test-Path -LiteralPath $catalogPath)) {
    Add-Failure 'Missing flow catalog: powerplatform/src/flows/flow-catalog.yaml'
} else {
    $catalogEntries = @(Get-FlowCatalogEntries -Path $catalogPath)
    if ($catalogEntries.Count -lt 1) {
        Add-Failure 'Flow catalog does not contain any flow entries.'
    }

    $catalogContent = Get-Content -LiteralPath $catalogPath -Raw

    $catalogEnvVars = @{}
    if ($DryRun) {
        foreach ($entry in $catalogEntries) {
            $envPattern = [regex]::Matches($catalogContent, "(?m)^\s{6}-\s+(decisions_\w+)")
            foreach ($match in $envPattern) {
                $catalogEnvVars[$match.Groups[1].Value] = $true
            }
        }
    }

    foreach ($entry in $catalogEntries) {
        if ([string]::IsNullOrWhiteSpace($entry.File)) {
            Add-Failure "Flow catalog entry '$($entry.Name)' is missing a file path."
            continue
        }

        $flowPath = Join-Path $repoRoot $entry.File
        if (-not (Test-Path -LiteralPath $flowPath)) {
            Add-Failure "Flow spec listed in catalog does not exist: $($entry.File)"
            continue
        }

        Test-FlowSpec -Path $flowPath -Name $entry.Name

        if ($DryRun) {
            $flowContent = Get-Content -LiteralPath $flowPath -Raw
            $referencedEnvVars = [regex]::Matches($flowContent, 'decisions_\w+')
            $referencedVarNames = $referencedEnvVars | ForEach-Object { $_.Value } | Sort-Object -Unique
            foreach ($varName in $referencedVarNames) {
                if (-not $catalogEnvVars.ContainsKey($varName)) {
                    Add-Warning "Flow '$($entry.Name)' references environment variable '$varName' that is not listed in the flow catalog."
                }
            }
        }
    }

    foreach ($candidate in @(
        'fictional-planner-plan',
        '11111111-1111-4111-8111-111111111111',
        'https://example.invalid/sites/decisions'
    )) {
        if ($catalogContent -match [regex]::Escape($candidate)) {
            $candidateBlockPattern = [regex]::Escape($candidate) + '[\s\S]{0,120}scope:\s*optional local dev-space binding'
            if ($catalogContent -notmatch $candidateBlockPattern) {
                Add-Failure "Catalog DMS candidate '$candidate' is not marked as an optional local dev-space binding."
            }
        }
    }
}

if (-not (Test-Path -LiteralPath $implementationChecklistPath)) {
    Add-Failure 'Missing flow implementation checklist: powerplatform/src/flows/implementation-checklist.json'
} else {
    $implementationChecklist = Get-Content -LiteralPath $implementationChecklistPath -Raw
    foreach ($pattern in @(
        'without adding live write steps outside the intended business process',
        'without a live production change',
        'against sample or replay data',
        'planner-sync-manual-replay',
        'outlook-invite-sync-dry-run',
        'teams-summary-post-preview',
        'org-hierarchy-sync-manual-refresh',
        'ai-transcript-minutes-sample-run'
    )) {
        if ($implementationChecklist -notmatch [regex]::Escape($pattern)) {
            Add-Failure "Flow implementation checklist is missing required gate text: $pattern"
        }
    }
}

if ($PassThru) {
    [pscustomobject]@{
        Passed    = ($failures.Count -eq 0)
        Failures  = @($failures)
        Warnings  = @($warnings)
        Summary   = "Flow contract validation with $($failures.Count) failures and $($warnings.Count) warnings."
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

Write-Host "Flow contract validation passed. ($($failures.Count) failures, $($warnings.Count) warnings)"

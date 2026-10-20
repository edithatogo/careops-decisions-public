[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$failures = [System.Collections.Generic.List[string]]::new()

function Add-Check {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [bool]$Passed,
        [Parameter(Mandatory = $true)]
        [string]$Detail
    )

    $prefix = if ($Passed) { '[PASS]' } else { '[FAIL]' }
    Write-Host "$prefix $Name - $Detail"
    if (-not $Passed) {
        $script:failures.Add("$Name - $Detail")
    }
}

function Read-JsonObject {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $path = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing required credentialing contract file: $RelativePath"
    }
    return Get-Content -LiteralPath $path -Raw | ConvertFrom-Json -Depth 100
}

function Get-Step {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Contract,
        [Parameter(Mandatory = $true)]
        [string]$Id
    )

    return @($Contract.steps | Where-Object { $_.id -eq $Id }) | Select-Object -First 1
}

function Test-RequiredProperties {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Value,
        [Parameter(Mandatory = $true)]
        [string[]]$Required,
        [Parameter(Mandatory = $true)]
        [string]$Context
    )

    $properties = @($Value.PSObject.Properties.Name)
    foreach ($name in $Required) {
        Add-Check -Name "$Context property $name" -Passed ($properties -contains $name) -Detail 'required property present'
    }
}

function Get-NegativePathOutcome {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Case
    )

    if ($Case.unmanagedConflictPresent -eq $true -and $Case.recusalEvidencePresent -ne $true) {
        return 'blocked-conflict-control'
    }
    if ($Case.pathwayCode -in @('interim', 'emergency', 'disaster') -and [string]::IsNullOrWhiteSpace([string]$Case.temporaryApprovalExpiry)) {
        return 'blocked-hard-expiry-required'
    }
    if ($Case.newAdverseMaterialConsidered -eq $true -and $Case.proceduralFairnessStatus -notin @('response-considered', 'complete')) {
        return 'blocked-procedural-fairness'
    }
    if ($Case.recommendationAutomaticallyCreatesDecision -eq $true) {
        return 'blocked-separate-decision-required'
    }
    if ($Case.aiDecisionAttempted -eq $true -or $Case.humanDecisionConfirmed -ne $true) {
        return 'blocked-human-decision-required'
    }
    if ($Case.currentDelegationConfirmed -eq $false) {
        return 'blocked-current-delegation-required'
    }
    return 'unexpected-pass'
}

$governanceContract = Read-JsonObject 'powerplatform/src/flows/credentialing-governance.contract.json'
$packetSchema = Read-JsonObject 'powerplatform/src/contracts/credentialing-governance-packet.schema.json'
$resultSchema = Read-JsonObject 'powerplatform/src/contracts/credentialing-result.schema.json'
$legacyContract = Read-JsonObject 'powerplatform/src/contracts/legacy-governance-reference.contract.json'
$legacySchema = Read-JsonObject 'powerplatform/src/contracts/legacy-governance-reference.schema.json'
$validFixture = Read-JsonObject 'tests/fixtures/credentialing/routine-renewal.valid.json'
$negativeFixture = Read-JsonObject 'tests/fixtures/credentialing/governance-negative-paths.json'
$legacyFixture = Read-JsonObject 'tests/fixtures/credentialing/legacy-reference.valid.json'

Add-Check -Name 'governance capability remains disabled' -Passed ($governanceContract.status -eq 'blueprint-disabled-by-default') -Detail ([string]$governanceContract.status)
Add-Check -Name 'governance contract version' -Passed ($governanceContract.contractVersion -eq '0.1.0') -Detail ([string]$governanceContract.contractVersion)
Add-Check -Name 'governance feature flag exists' -Passed ($governanceContract.featureFlag -eq 'decisions_EnableCredentialingGovernance') -Detail ([string]$governanceContract.featureFlag)

$requiredStepIds = @(
    'receive-governance-packet',
    'verify-packet',
    'resolve-governance-context',
    'check-case-readiness',
    'confirm-quorum-and-conflicts',
    'confirm-procedural-fairness',
    'record-human-recommendation',
    'route-to-current-delegate',
    'record-human-delegated-decision',
    'return-result-references',
    'emit-process-events',
    'finalize-governance-record'
)
$observedStepIds = @($governanceContract.steps | ForEach-Object { [string]$_.id })
foreach ($stepId in $requiredStepIds) {
    Add-Check -Name "governance step $stepId" -Passed ($observedStepIds -contains $stepId) -Detail 'required step present'
}
Add-Check -Name 'governance step IDs unique' -Passed (($observedStepIds | Select-Object -Unique).Count -eq $observedStepIds.Count) -Detail "count=$($observedStepIds.Count)"

$recommendationStep = Get-Step -Contract $governanceContract -Id 'record-human-recommendation'
Add-Check -Name 'recommendation remains human' -Passed ($recommendationStep.aiCannotConfirm -eq $true) -Detail 'AI cannot confirm recommendation'
Add-Check -Name 'recommendation is not a decision' -Passed ($recommendationStep.doesNotCreateDecision -eq $true) -Detail 'separation preserved'

$decisionStep = Get-Step -Contract $governanceContract -Id 'record-human-delegated-decision'
Add-Check -Name 'decision remains human' -Passed ($decisionStep.aiCannotDecide -eq $true -and $decisionStep.automaticDecisionPermitted -eq $false) -Detail 'automatic and AI decisions prohibited'
Add-Check -Name 'human decision confirmation required' -Passed (@($decisionStep.requires) -contains 'humanDecisionConfirmed') -Detail 'required field present'
Add-Check -Name 'delegation reference required' -Passed (@($decisionStep.requires) -contains 'delegationAuthorityReference') -Detail 'required field present'

$resultStep = Get-Step -Contract $governanceContract -Id 'return-result-references'
Add-Check -Name 'result does not replace register' -Passed ($resultStep.doesNotReplaceAuthoritativeRegister -eq $true) -Detail 'register boundary preserved'

$requiredFailurePaths = @(
    'missing-authority-blocks-consideration-or-decision',
    'missing-quorum-or-relevant-peer-defers-consideration',
    'conflict-without-recusal-blocks-participation',
    'adverse-path-without-fairness-blocks-finalisation',
    'recommendation-never-auto-creates-decision',
    'ai-output-never-confirms-recommendation-or-decision',
    'temporary-pathway-without-hard-expiry-is-rejected'
)
foreach ($failurePath in $requiredFailurePaths) {
    Add-Check -Name "failure path $failurePath" -Passed (@($governanceContract.failurePaths) -contains $failurePath) -Detail 'required failure path present'
}

Add-Check -Name 'packet schema contract version' -Passed ($packetSchema.properties.contractVersion.const -eq '0.1.0') -Detail 'schema version pinned'
Add-Check -Name 'packet schema forbids extra fields' -Passed ($packetSchema.additionalProperties -eq $false) -Detail 'fail-closed schema'
Add-Check -Name 'temporary packet expiry rule present' -Passed (@($packetSchema.allOf).Count -ge 1) -Detail 'conditional expiry rule present'

Add-Check -Name 'result schema requires human decision' -Passed ($resultSchema.properties.humanDecisionConfirmed.const -eq $true) -Detail 'human decision is mandatory'
Add-Check -Name 'result schema prohibits register write' -Passed ($resultSchema.properties.authoritativeRegisterUpdated.const -eq $false) -Detail 'MVP remains non-authoritative'
Add-Check -Name 'result schema prohibits release-to-practise confirmation' -Passed ($resultSchema.properties.releaseToPractiseConfirmed.const -eq $false) -Detail 'downstream control remains separate'

Add-Check -Name 'legacy contract disabled' -Passed ($legacyContract.enabled -eq $false) -Detail 'legacy reference disabled by default'
Add-Check -Name 'legacy contract human review' -Passed ($legacyContract.humanReviewRequired -eq $true) -Detail 'review required'
Add-Check -Name 'legacy contract non-authoritative' -Passed ($legacyContract.authoritativeDecision -eq $false -and $legacyContract.productionWrites -eq $false) -Detail 'no authority or writes'
Add-Check -Name 'legacy schema non-authoritative' -Passed ($legacySchema.properties.authoritativeDecision.const -eq $false -and $legacySchema.properties.productionWrite.const -eq $false) -Detail 'schema enforces boundary'

$packetRequired = @($packetSchema.required | ForEach-Object { [string]$_ })
Test-RequiredProperties -Value $validFixture.governancePacket -Required $packetRequired -Context 'valid packet'
$resultRequired = @($resultSchema.required | ForEach-Object { [string]$_ })
Test-RequiredProperties -Value $validFixture.decision -Required $resultRequired -Context 'valid result'
$legacyRequired = @($legacySchema.required | ForEach-Object { [string]$_ })
Test-RequiredProperties -Value $legacyFixture -Required $legacyRequired -Context 'valid legacy reference'

Add-Check -Name 'valid fixture synthetic' -Passed ($validFixture.classification -eq 'synthetic-non-production') -Detail ([string]$validFixture.classification)
Add-Check -Name 'valid fixture separates recommendation and decision' -Passed ($validFixture.recommendation.doesNotCreateDecision -eq $true -and $validFixture.decision.humanDecisionConfirmed -eq $true) -Detail 'separate human records present'
Add-Check -Name 'valid fixture remains non-authoritative' -Passed ($validFixture.authoritativeWriteExpected -eq $false -and $validFixture.releaseToPractiseExpected -eq $false) -Detail 'no register or release effect'
Add-Check -Name 'valid fixture excludes AI decision' -Passed ($validFixture.aiDecisionExpected -eq $false) -Detail 'human decision only'

foreach ($case in @($negativeFixture.cases)) {
    $actual = Get-NegativePathOutcome -Case $case
    Add-Check -Name "negative path $($case.id)" -Passed ($actual -eq $case.expected) -Detail "expected=$($case.expected); actual=$actual"
}

Add-Check -Name 'legacy fixture remains historical only' -Passed ($legacyFixture.authoritativeDecision -eq $false -and $legacyFixture.currentRegistrationEstablished -eq $false -and $legacyFixture.currentScopeEstablished -eq $false) -Detail 'no current fact or authority inferred'

if ($failures.Count -gt 0) {
    throw "$($failures.Count) credentialing contract check(s) failed."
}

Write-Host 'Credentialing contract validation passed.'

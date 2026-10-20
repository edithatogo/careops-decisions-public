[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$repo = Get-Location

function Get-RepoPath {
    param([Parameter(Mandatory = $true)][string]$RelativePath)
    return Join-Path $repoRoot $RelativePath
}

function Assert-FileContains {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string[]]$Patterns
    )

    $fullPath = Get-RepoPath $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw "Missing deployment hardening artifact: $RelativePath"
    }

    $content = Get-Content -LiteralPath $fullPath -Raw
    if ([string]::IsNullOrWhiteSpace($content)) {
        throw "Deployment hardening artifact is empty: $RelativePath"
    }

    foreach ($pattern in $Patterns) {
        if ($content -notmatch [regex]::Escape($pattern)) {
            throw "$RelativePath is missing required text: $pattern"
        }
    }

    return $content
}

function Assert-JsonHasProperty {
    param(
        [Parameter(Mandatory = $true)][psobject]$Object,
        [Parameter(Mandatory = $true)][string]$PropertyName,
        [Parameter(Mandatory = $true)][string]$Context
    )

    if (-not $Object.PSObject.Properties[$PropertyName]) {
        throw "$Context is missing required JSON property: $PropertyName"
    }
}

function Assert-JsonValue {
    param(
        [Parameter(Mandatory = $true)]$Actual,
        [Parameter(Mandatory = $true)]$Expected,
        [Parameter(Mandatory = $true)][string]$Context
    )

    if ("$Actual" -ne "$Expected") {
        throw "$Context must be '$Expected' but was '$Actual'."
    }
}

function Assert-MetadataWorkstreamDeferred {
    param([Parameter(Mandatory = $true)]$TrackMetadata)

    $workstreams = $TrackMetadata.workstreams
    if (-not $workstreams) {
        throw 'Track 13 metadata must include workstreams.'
    }

    if (-not $workstreams.cliBindings -or -not $workstreams.deploymentProfiles -or
        -not $workstreams.aiBuilderGovernance -or -not $workstreams.documentStorage -or
        -not $workstreams.toolingMapUpdate -or -not $workstreams.validationExpansion -or
        -not $workstreams.reseedRunbook) {
        throw 'Track 13 metadata is missing required implemented-workstream coverage keys.'
    }

    $requiredDeferred = @('solutionAlm', 'securityRoles', 'mvpAppBuild', 'firstIntegrationFlow')
    foreach ($stream in $requiredDeferred) {
        if (-not $workstreams.$stream) {
            throw "Track 13 metadata is missing deferred workstream marker: $stream"
        }
        if ($workstreams.$stream -notmatch 'deferred|design_only|gated') {
            throw "Track 13 deferred workstream '$stream' must explicitly include deferred/gated status text."
        }
    }
}

$trackMetadataPath = Get-RepoPath 'conductor/tracks/track-13-deployment-hardening-alm-entra-readiness_20260502/metadata.json'
if (-not (Test-Path -LiteralPath $trackMetadataPath)) {
    throw "Missing Track 13 metadata: $trackMetadataPath"
}
$trackMetadata = Get-Content -LiteralPath $trackMetadataPath -Raw | ConvertFrom-Json
Assert-JsonHasProperty -Object $trackMetadata -PropertyName 'status' -Context 'Track 13 metadata'
Assert-JsonValue -Actual $trackMetadata.status -Expected 'in_progress' -Context 'Track 13 status'
Assert-MetadataWorkstreamDeferred -TrackMetadata $trackMetadata
if ($trackMetadata.closedOn) {
    throw 'Track 13 should remain open; closedOn should not be set while blocked.'
}

Assert-FileContains -RelativePath 'docs/deployment/pac-cli-reference.md' -Patterns @(
    'pac solution export',
    'pac solution unpack',
    'pac solution pack',
    'pacx table list',
    'pac solution import'
)

Assert-FileContains -RelativePath 'docs/deployment/deployment-profiles.md' -Patterns @(
    '# Deployment Profiles',
    'Profile',
    'local',
    'development',
    'test',
    'enterprise',
    'gated until'
)

Assert-FileContains -RelativePath 'docs/deployment/ai-builder-governance.md' -Patterns @(
    'AI Builder Governance',
    'Human Review States',
    'Audit Fields',
    'No AI Builder prompt is called without an active feature-switch flag',
    'AI-generated minutes cannot be published without a human review decision'
)

Assert-FileContains -RelativePath 'docs/deployment/document-storage-conventions.md' -Patterns @(
    '# Document Storage Conventions',
    'Document Types',
    'Current Status',
    'SharePoint document library paths are placeholders',
    'Dataverse-to-Document Library Mapping'
)

Assert-FileContains -RelativePath 'powerplatform/src/solution-component-manifest.yaml' -Patterns @(
    'schema: decisions.workspace.solutionComponentManifest.v1',
    'liveGates:',
    'securityRoleActivation:',
    'firstIntegrationFlow:',
    'azureEntraImplementation:'
)

Assert-FileContains -RelativePath 'docs/operations/bootstrap-runbook.md' -Patterns @(
    '# Bootstrap Runbook',
    'Data-Only Reseed',
    'reverse dependency order',
    'Re-run full bootstrap',
    'pacx data delete'
)

Assert-FileContains -RelativePath 'conductor/status.md' -Patterns @(
    'Track 13',
    'security-role activation',
    'first integration flow'
)

Assert-FileContains -RelativePath 'conductor/tracks/track-13-deployment-hardening-alm-entra-readiness_20260502/spec.md' -Patterns @(
    '# Track 13: Deployment Hardening',
    'ALM',
    'Azure/Entra',
    'design',
    'validation'
)

Assert-FileContains -RelativePath 'docs/implementation/decisions-feature-coverage.md' -Patterns @(
    'Security',
    'Access controls',
    'pending'
)

Write-Host "Track 13 deployment hardening contract validation passed."

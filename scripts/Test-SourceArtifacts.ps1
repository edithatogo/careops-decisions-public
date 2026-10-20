param(
    [switch]$RequireImplementationScaffolds
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

$jsonFiles = @(
    'powerplatform/src/dataverse-model.json',
    'powerplatform/src/seed/sample-governance-data.json',
    'powerplatform/settings/deployment-settings.sample.json',
    'powerplatform/src/schema/schema-build-plan.json'
    'powerplatform/generated/dataverse/bootstrap-manifest.json'
)

foreach ($relativePath in $jsonFiles) {
    $fullPath = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw "Missing JSON artifact: $relativePath"
    }

    Get-Content -LiteralPath $fullPath -Raw | ConvertFrom-Json | Out-Null
    Write-Host "Valid JSON: $relativePath"
}

function Resolve-FirstExistingArtifact {
    param(
        [Parameter(Mandatory = $true)][string[]]$Candidates,
        [Parameter(Mandatory = $true)][string]$Label
    )

    foreach ($candidate in $Candidates) {
        $fullPath = Join-Path $root $candidate
        if (Test-Path -LiteralPath $fullPath) {
            return $candidate
        }
    }

    throw "Missing XML artifact for ${Label}: expected one of $($Candidates -join ', ')"
}

$xmlFiles = @()
$xmlFiles += Resolve-FirstExistingArtifact -Label 'solution manifest' -Candidates @(
    'powerplatform/solutions/DecisionsWorkspace/Other/Solution.xml',
    'powerplatform/solutions/DecisionsWorkspace/src/Other/Solution.xml',
    'powerplatform/solutions/DecisionsWorkspace/Solution.xml'
)
$xmlFiles += Resolve-FirstExistingArtifact -Label 'customizations' -Candidates @(
    'powerplatform/solutions/DecisionsWorkspace/Other/Customizations.xml',
    'powerplatform/solutions/DecisionsWorkspace/src/Other/Customizations.xml'
)
$xmlFiles += Resolve-FirstExistingArtifact -Label 'relationships' -Candidates @(
    'powerplatform/solutions/DecisionsWorkspace/Other/Relationships.xml',
    'powerplatform/solutions/DecisionsWorkspace/src/Other/Relationships.xml'
)

foreach ($relativePath in ($xmlFiles | Select-Object -Unique)) {
    $fullPath = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw "Missing XML artifact: $relativePath"
    }

    [xml](Get-Content -LiteralPath $fullPath -Raw) | Out-Null
    Write-Host "Valid XML: $relativePath"
}

$implementationScaffolds = @(
    'powerplatform/src/schema/dataverse-tables.yaml',
    'powerplatform/src/schema/relationships.yaml',
    'powerplatform/src/schema/choices.yaml',
    'powerplatform/src/schema/schema-build-plan.json',
    'powerplatform/src/solution-component-manifest.yaml',
    'powerplatform/src/app/app-map.yaml',
    'powerplatform/src/app/components.yaml',
    'powerplatform/src/app/commands.yaml',
    'powerplatform/src/app/access-matrix.yaml',
    'powerplatform/src/app/screens.md',
    'powerplatform/src/app/powerfx-patterns.md',
    'powerplatform/src/flows/flow-catalog.yaml',
    'powerplatform/src/flows/planner-sync.yaml',
    'powerplatform/src/flows/outlook-invite-sync.yaml',
    'powerplatform/src/flows/teams-summary-post.yaml',
    'powerplatform/src/flows/org-hierarchy-sync.yaml',
    'powerplatform/src/flows/ai-transcript-minutes.yaml',
    'powerplatform/src/reporting/README.md',
    'powerplatform/src/reporting/dataverse-dashboards.yaml',
    'powerplatform/generated/dataverse/README.md',
    'powerplatform/generated/dataverse/bootstrap.pacx.ps1',
    'tests/powerapps/DecisionsWorkspace.testplan.yaml'
)

foreach ($relativePath in $implementationScaffolds) {
    $fullPath = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        if ($RequireImplementationScaffolds) {
            throw "Missing implementation scaffold: $relativePath"
        }

        Write-Warning "Implementation scaffold not present yet: $relativePath"
        continue
    }

    $content = Get-Content -LiteralPath $fullPath -Raw
    if ([string]::IsNullOrWhiteSpace($content)) {
        throw "Implementation scaffold is empty: $relativePath"
    }

    Write-Host "Present scaffold: $relativePath"
}

$reportingReadme = Get-Content -LiteralPath (Join-Path $root 'powerplatform/src/reporting/README.md') -Raw
if ($reportingReadme -notmatch [regex]::Escape('Dataverse-first reporting surfaces')) {
    throw 'Reporting README does not reference the Dataverse-first reporting surfaces contract.'
}
if ($reportingReadme -notmatch [regex]::Escape('Power BI and Fabric stay deferred')) {
    throw 'Reporting README does not keep Power BI and Fabric deferred.'
}

$reportingContract = Get-Content -LiteralPath (Join-Path $root 'powerplatform/src/reporting/dataverse-dashboards.yaml') -Raw
foreach ($pattern in @(
    'schema: decisions.workspace.reporting.v1',
    'secretary-workbench',
    'governance-audit'
)) {
    if ($reportingContract -notmatch [regex]::Escape($pattern)) {
        throw "Reporting contract does not reference required text: $pattern"
    }
}

Write-Host "Source artifact validation completed."

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

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
        throw "Missing agent workflow artifact: $RelativePath"
    }

    $content = Get-Content -LiteralPath $fullPath -Raw
    if ([string]::IsNullOrWhiteSpace($content)) {
        throw "Agent workflow artifact is empty: $RelativePath"
    }

    foreach ($pattern in $Patterns) {
        if ($content -notmatch [regex]::Escape($pattern)) {
            throw "$RelativePath does not reference required text: $pattern"
        }
    }

    return $content
}

$runbook = Assert-FileContains -RelativePath 'docs/operations/agent-runbook.md' -Patterns @(
    'Parallelize source work; serialize live Dylan writes',
    'Only one deployment operator should run live',
    'Real Medication Safety Committee records must come from NSW Outlook, SharePoint, Teams, or user-provided exported files',
    'Gmail is not a valid source',
    'Power Platform ALM Agent',
    'Model-Driven App UX Agent',
    'Dataverse Data Model Agent',
    'Workflow Integration Agent',
    'Governance and Safety Agent',
    'Medication Safety Secretariat Agent',
    'Testing and Review Agent',
    'Deployment Operator Agent',
    'Use writer/reviewer pairs for high-risk changes',
    'Keep write sets disjoint',
    'Each agent handoff must include the following evidence',
    'scripts/Verify-DylanDeployment.ps1',
    'operator@example.invalid',
    'One deployment operator runs the write',
    'One reviewer waits for completion and then runs read-only verification'
)

foreach ($pair in @(
    'Model-Driven App UX Agent | Testing and Review Agent',
    'Dataverse Data Model Agent | Governance and Safety Agent',
    'Workflow Integration Agent | Governance and Safety Agent',
    'Power Platform ALM Agent | Testing and Review Agent',
    'Deployment Operator Agent | Testing and Review Agent'
)) {
    if ($runbook -notmatch [regex]::Escape($pair)) {
        throw "docs/operations/agent-runbook.md is missing writer/reviewer pair: $pair"
    }
}

$verifier = Assert-FileContains -RelativePath 'scripts/Verify-DylanDeployment.ps1' -Patterns @(
    'CmdletBinding',
    'EnvironmentUrl = "https://environment.example.invalid/"',
    'AppId = "22222222-2222-4222-8222-222222222222"',
    'AppUniqueName = "decisions_workspace_64f7b61a"',
    'CheckMedicationSafetySample',
    'Json',
    'pac',
    'env',
    'who',
    'fetch',
    'model',
    'genpage',
    'list',
    'dft_meeting',
    'dft_agendaitem',
    'dft_paper',
    'dft_minutes',
    'dft_decision',
    'dft_actionitem',
    'dft_meetingfeatureoverride',
    'medication-safety-sample-meeting'
)

foreach ($forbidden in @(
    'solution import',
    'model genpage upload',
    'solution publish',
    'Invoke-RestMethod',
    'Method Post',
    'Method Patch',
    'Import-Solution.ps1',
    'Import-MedicationSafetySampleToDataverse.ps1'
)) {
    if ($verifier -match [regex]::Escape($forbidden)) {
        throw "scripts/Verify-DylanDeployment.ps1 must remain read-only but references: $forbidden"
    }
}

Assert-FileContains -RelativePath 'docs/operations/README.md' -Patterns @(
    'docs/operations/agent-runbook.md',
    'scripts/Verify-DylanDeployment.ps1',
    'live Dylan deployment serialization',
    'read-only Dylan verification'
) | Out-Null

Assert-FileContains -RelativePath 'docs/source-map.md' -Patterns @(
    'Agent workflow runbook',
    'Dylan deployment verifier'
) | Out-Null

Assert-FileContains -RelativePath 'docs/implementation-roadmap.md' -Patterns @(
    'Track 24: agent workflow operations, writer/reviewer pairing, live deployment serialization, and Dylan verification.'
) | Out-Null

Assert-FileContains -RelativePath 'conductor/tracks/track-24-agent-workflow-operations_20260504/spec.md' -Patterns @(
    'agent ownership',
    'writer/reviewer pairs',
    'serialized through one deployment operator',
    'real Medication Safety Committee source records remain gated'
) | Out-Null

Assert-FileContains -RelativePath 'conductor/tracks/track-24-agent-workflow-operations_20260504/plan.md' -Patterns @(
    'Support JSON output for CI or handoff logs.',
    'One deployment operator',
    'read-only verification',
    'Other agents continue local docs/tests only.'
) | Out-Null

Write-Host 'Agent workflow contract validation passed.'

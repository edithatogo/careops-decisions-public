[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$bpmnPath = Join-Path $repoRoot 'docs/process/credentialing-governance-mvp.bpmn'
$mermaidPath = Join-Path $repoRoot 'docs/process/credentialing-governance-mvp.mmd'

if (-not (Test-Path -LiteralPath $bpmnPath -PathType Leaf)) {
    throw 'Missing credentialing BPMN model.'
}
if (-not (Test-Path -LiteralPath $mermaidPath -PathType Leaf)) {
    throw 'Missing credentialing Mermaid model.'
}

[xml]$document = Get-Content -LiteralPath $bpmnPath -Raw
$process = $document.SelectSingleNode("//*[local-name()='process']")
if ($null -eq $process) {
    throw 'BPMN model contains no process.'
}
if ($process.GetAttribute('isExecutable') -ne 'false') {
    throw 'Credentialing BPMN must remain executable-neutral.'
}

$requiredIds = @(
    'Task_ValidatePacket',
    'Task_ResolveContext',
    'Gateway_MeetingReady',
    'Task_Deliberation',
    'Task_Fairness',
    'Task_Recommendation',
    'Task_Delegate',
    'Task_Decision',
    'Task_Communication',
    'Task_Result',
    'Task_Events',
    'Task_Finalise',
    'EndEvent_Result'
)

$ids = @(
    $document.SelectNodes('//*[@id]') |
        ForEach-Object { $_.GetAttribute('id') }
)
foreach ($requiredId in $requiredIds) {
    if ($ids -notcontains $requiredId) {
        throw "BPMN model is missing required element: $requiredId"
    }
}
if (($ids | Select-Object -Unique).Count -ne $ids.Count) {
    throw 'BPMN model contains duplicate element IDs.'
}

$sequenceFlows = @($document.SelectNodes("//*[local-name()='sequenceFlow']"))
if ($sequenceFlows.Count -lt 20) {
    throw 'BPMN model does not contain the expected end-to-end flow.'
}
foreach ($flow in $sequenceFlows) {
    if ([string]::IsNullOrWhiteSpace($flow.GetAttribute('sourceRef')) -or [string]::IsNullOrWhiteSpace($flow.GetAttribute('targetRef'))) {
        throw "BPMN sequence flow has an empty source or target: $($flow.GetAttribute('id'))"
    }
    if ($ids -notcontains $flow.GetAttribute('sourceRef')) {
        throw "BPMN sequence flow has an unknown source: $($flow.GetAttribute('id'))"
    }
    if ($ids -notcontains $flow.GetAttribute('targetRef')) {
        throw "BPMN sequence flow has an unknown target: $($flow.GetAttribute('id'))"
    }
}

$mermaid = Get-Content -LiteralPath $mermaidPath -Raw
foreach ($text in @(
    'Human committee deliberation',
    'Complete procedural fairness',
    'Record human recommendation',
    'Record separate delegated decision',
    'Downstream register and release control remains separate'
)) {
    if ($mermaid -notmatch [regex]::Escape($text)) {
        throw "Mermaid model is missing required control text: $text"
    }
}

Write-Host 'Credentialing BPMN and Mermaid process-model validation passed.'

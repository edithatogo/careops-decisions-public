# Execute the actual renderer under both date cultures; no file/tenant writes.
$ErrorActionPreference = 'Stop'
$tokens = $null; $errors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'New-DeploymentChecklist.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count -gt 0) { throw 'Checklist source does not parse.' }
$definition = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'New-ChecklistMarkdown' }, $true)
if ($null -eq $definition) { throw 'Checklist renderer is absent.' }
. ([scriptblock]::Create($definition.Extent.Text))
$priorCulture = [System.Globalization.CultureInfo]::CurrentCulture
try {
    foreach ($culture in @('en-AU', 'en-US')) {
        [System.Globalization.CultureInfo]::CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo($culture)
        $utc = [datetime]::SpecifyKind([datetime]::new(2026, 10, 18, 12, 34, 56), [DateTimeKind]::Utc)
        $offset = [datetimeoffset]::new(2026, 10, 18, 12, 34, 56, [timespan]::FromHours(10))
        foreach ($dateCase in @(
            @{ Value = $utc; Expected = '2026-10-18 12:34:56 +00:00' },
            @{ Value = '2026-10-18T12:34:56+10:00'; Expected = '2026-10-18 12:34:56 +10:00' },
            @{ Value = $offset; Expected = '2026-10-18 12:34:56 +10:00' }
        )) {
            $record = [pscustomobject]@{ GeneratedOn = $dateCase.Value; Readiness = 'Synthetic'; Checks = @() }
            $markdown = New-ChecklistMarkdown -Readiness $record
            if ($markdown -notmatch [regex]::Escape('Generated: ' + $dateCase.Expected)) {
                throw "Checklist date or offset changed under $culture."
            }
        }
    }
} finally {
    [System.Globalization.CultureInfo]::CurrentCulture = $priorCulture
}
Write-Host 'Six actual renderer date/culture/offset cases passed.'

# Exercise the complete dry-run entry point with PAC present but forbidden.
# This detects transitive auth reads that renderer-only tests cannot observe.
$global:ChecklistPacCalls = 0
function global:pac {
    $global:ChecklistPacCalls++
    throw 'Checklist dry run attempted PAC transport/authentication.'
}
$tempOutput = Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString() + '.md')
try {
    $result = & (Join-Path $PSScriptRoot 'New-DeploymentChecklist.ps1') -DryRun -OutputPath $tempOutput
    if ($global:ChecklistPacCalls -ne 0) { throw 'Checklist dry run called PAC.' }
    if (Test-Path -LiteralPath $tempOutput) { throw 'Checklist dry run wrote its output.' }
    if ($result.Markdown -match 'pac solution import' -or $result.Markdown -match '--publish-changes') {
        throw 'Checklist bypasses guarded target handling or opts into publishing.'
    }
    if ($result.Markdown -notmatch 'Import-Solution.ps1.*-EnvironmentUrl.*-DryRun') {
        throw 'Checklist must show an explicit-target offline import preview.'
    }
} finally {
    Remove-Item Function:\pac -ErrorAction SilentlyContinue
    Remove-Variable ChecklistPacCalls -Scope Global -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $tempOutput -ErrorAction SilentlyContinue
}
Write-Host 'Complete checklist dry-run refuses auth transport and preserves the filesystem.'

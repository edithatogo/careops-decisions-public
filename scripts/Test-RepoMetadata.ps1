param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$failures = New-Object System.Collections.Generic.List[string]

function Require-File {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $path = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing repository metadata file: $RelativePath")
        return $null
    }

    $content = Get-Content -LiteralPath $path -Raw
    if ([string]::IsNullOrWhiteSpace($content)) {
        $failures.Add("Empty repository metadata file: $RelativePath")
    }

    return $content
}

$license = Require-File -RelativePath 'LICENSE'
$version = Require-File -RelativePath 'VERSION'
$changelog = Require-File -RelativePath 'CHANGELOG.md'
$notice = Require-File -RelativePath 'NOTICE'

if ($license -and $license -notmatch 'Apache License\s+Version 2\.0') {
    $failures.Add('LICENSE is not Apache License 2.0.')
}

if ($version -and $version.Trim() -notmatch '^\d+\.\d+\.\d+$') {
    $failures.Add('VERSION must be SemVer core format, for example 0.1.0.')
}

if ($version -and $changelog) {
    $escapedVersion = [regex]::Escape($version.Trim())
    if ($changelog -notmatch "##\s+$escapedVersion\b") {
        $failures.Add("CHANGELOG.md does not contain an entry for VERSION $($version.Trim()).")
    }
}

if ($notice -and $notice -notmatch 'CareOps Decisions') {
    $failures.Add('NOTICE does not identify CareOps Decisions.')
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Repository metadata validation completed."

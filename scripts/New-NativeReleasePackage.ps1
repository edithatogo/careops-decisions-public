[CmdletBinding()]
param(
    [Parameter(Mandatory)] [ValidatePattern('^[a-f0-9]{40}$')] [string] $SourceCommit,
    [string] $OutputDirectory = '.local/native-release'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if ((git -C $root rev-parse HEAD) -cne $SourceCommit) { throw 'Package source must equal the checked-out commit.' }
$output = [IO.Path]::GetFullPath((Join-Path $root $OutputDirectory))
$stage = Join-Path $output 'payload'
$archive = Join-Path $output 'careops-decisions-native.zip'
if ((Test-Path $stage) -or (Test-Path $archive)) { throw 'Existing package output must be retained; choose a fresh output directory.' }
$libraries = @(
    @{ Name = 'Authority'; Framework = 'net462' },
    @{ Name = 'Calendars'; Framework = 'net462' },
    @{ Name = 'RegisterSchema'; Framework = 'net48' },
    @{ Name = 'DataverseIdentity'; Framework = 'net48' }
)
# Inspect every real CLR assembly before creating a release payload.
$entries = @()
foreach ($library in $libraries) {
    $name = "CareOps.Decisions.$($library.Name)"
    $bin = Join-Path $root "powerplatform/native/$($library.Name)/bin/Release/$($library.Framework)"
    $dll = Join-Path $bin "$name.dll"
    if (-not (Test-Path -LiteralPath $dll -PathType Leaf)) { throw "Missing compiled assembly: $name" }
    $identity = [Reflection.AssemblyName]::GetAssemblyName($dll)
    if ($identity.Name -cne $name) { throw "Wrong compiled assembly identity: $name" }
    $xml = Join-Path $bin "$name.xml"
    [xml] $documentation = Get-Content -LiteralPath $xml -Raw
    if ($documentation.doc.assembly.name -cne $name) { throw "Wrong API documentation identity: $name" }
    $entries += @{ Name = $name; Dll = $dll; Xml = $xml; Framework = $library.Framework }
}
New-Item -ItemType Directory -Path (Join-Path $stage 'lib') -Force | Out-Null
$manifest = [ordered]@{ sourceCommit = $SourceCommit; assemblies = @(); thirdPartyBinariesIncluded = $false; nativeDeploymentQualified = $false }
foreach ($entry in $entries) {
    foreach ($path in @($entry.Dll, $entry.Xml)) { Copy-Item -LiteralPath $path -Destination (Join-Path $stage 'lib') }
    $manifest.assemblies += @{ name = $entry.Name; framework = $entry.Framework; sha256 = (Get-FileHash -LiteralPath $entry.Dll -Algorithm SHA256).Hash.ToLowerInvariant() }
}
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $stage 'native-release.json') -Encoding utf8
@'
# CareOps Decisions native libraries

The lib directory contains the four project-owned compiled CLR libraries and
their API XML. Use Windows .NET Framework 4.8 for the complete set. Source,
project declarations and exact NuGet locks are in source.zip. Restore those
dependencies separately; this archive does not redistribute third-party DLLs
or claim complete binary dependency closure. Preserve their own licence terms.

This is an offline library/source release. Dataverse registration, strong-name
requirements, environment bindings, solution composition and operator acceptance
remain separate. It does not install, activate, connect to or modify a tenant.
'@ | Set-Content -LiteralPath (Join-Path $stage 'README.md') -Encoding utf8
$sourceArchive = Join-Path $stage 'source.zip'
git -C $root -c core.autocrlf=false -c core.eol=lf archive --format=zip "--output=$sourceArchive" $SourceCommit
if ($LASTEXITCODE -ne 0) { throw 'Exact-source archive failed.' }
$source = [IO.Compression.ZipFile]::OpenRead($sourceArchive)
try {
    foreach ($file in @('LICENSE', 'global.json')) {
        $entryStream = $source.GetEntry($file).Open()
        $destinationStream = [IO.File]::Create((Join-Path $stage $file))
        try { $entryStream.CopyTo($destinationStream) }
        finally { $destinationStream.Dispose(); $entryStream.Dispose() }
    }
} finally { $source.Dispose() }
[IO.Compression.ZipFile]::CreateFromDirectory($stage, $archive)
Write-Output $archive

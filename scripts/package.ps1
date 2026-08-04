[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [string]$Version = 'dev',

    [string]$OutputDirectory = 'artifacts',

    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot 'HeroShift - SRC Files/HeroShift.sln'
$targetFramework = 'net10.0'
$buildOutput = Join-Path $repoRoot "HeroShift - SRC Files/bin/$Configuration/$targetFramework"
$gamedata = Join-Path $repoRoot 'HeroShift - SRC Files/src/gamedata/HeroShift.gamedata.json'
$defaultConfig = Join-Path $repoRoot 'packaging/HeroShift/configs/heroshift.json'
$outputRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDirectory))
$stageRoot = Join-Path $outputRoot "stage/HeroShift-$Version"
$zipPath = Join-Path $outputRoot "HeroShift-$Version.zip"
$manifestPath = Join-Path $stageRoot 'package-manifest.json'

if (-not $NoBuild) {
    & dotnet build $solution -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE" }
}

$requiredSources = [ordered]@{
    'plugins/HeroShift/HeroShift.dll' = Join-Path $buildOutput 'HeroShift.dll'
    'plugins/HeroShift/WASDMenuAPI.dll' = Join-Path $buildOutput 'WASDMenuAPI.dll'
    'plugins/HeroShift/Newtonsoft.Json.dll' = Join-Path $buildOutput 'Newtonsoft.Json.dll'
    'plugins/HeroShift/configs/heroshift.json' = $defaultConfig
    'gamedata/HeroShift.gamedata.json' = $gamedata
}

foreach ($source in $requiredSources.Values) {
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Required package source is missing: $source"
    }
}

if (Test-Path -LiteralPath $stageRoot) {
    Remove-Item -LiteralPath $stageRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $stageRoot -Force | Out-Null

foreach ($entry in $requiredSources.GetEnumerator()) {
    $destination = Join-Path $stageRoot ($entry.Key.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
    New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
    Copy-Item -LiteralPath $entry.Value -Destination $destination -Force
}

$manifestEntries = @()
foreach ($relativePath in ($requiredSources.Keys | Sort-Object)) {
    $filePath = Join-Path $stageRoot ($relativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
    $item = Get-Item -LiteralPath $filePath
    $manifestEntries += [ordered]@{
        path = $relativePath
        size = $item.Length
        sha256 = (Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}

$manifest = [ordered]@{
    schemaVersion = 1
    package = 'HeroShift'
    version = $Version
    targetFramework = $targetFramework
    files = $manifestEntries
    externalDependencies = @(
        [ordered]@{ name = 'RayTraceApi'; path = 'shared/RayTraceApi/RayTraceApi.dll' },
        [ordered]@{ name = 'RayTraceImpl'; path = 'plugins/RayTraceImpl/RayTraceImpl.dll' },
        [ordered]@{ name = 'RayTrace MetaMod'; path = 'metamod/RayTrace.vdf' }
    )
}
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding utf8NoBOM

$actualFiles = Get-ChildItem -LiteralPath $stageRoot -File -Recurse | ForEach-Object {
    [System.IO.Path]::GetRelativePath($stageRoot, $_.FullName).Replace('\', '/')
} | Sort-Object
$expectedFiles = @($requiredSources.Keys) + 'package-manifest.json' | Sort-Object
if (Compare-Object -ReferenceObject $expectedFiles -DifferenceObject $actualFiles) {
    throw 'Package inventory does not match the expected file list.'
}
if ($actualFiles | Where-Object { $_ -match '\.(pdb|xml)$' }) {
    throw 'Debug or documentation files leaked into the package.'
}

if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$fixedTimestamp = [DateTimeOffset]::new(1980, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
$stream = [System.IO.File]::Open($zipPath, [System.IO.FileMode]::CreateNew)
try {
    $archive = [System.IO.Compression.ZipArchive]::new($stream, [System.IO.Compression.ZipArchiveMode]::Create, $false)
    try {
        foreach ($relativePath in $actualFiles) {
            $source = Join-Path $stageRoot ($relativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
            $entry = $archive.CreateEntry($relativePath, [System.IO.Compression.CompressionLevel]::Optimal)
            $entry.LastWriteTime = $fixedTimestamp
            $entryStream = $entry.Open()
            try {
                $sourceStream = [System.IO.File]::OpenRead($source)
                try { $sourceStream.CopyTo($entryStream) } finally { $sourceStream.Dispose() }
            } finally { $entryStream.Dispose() }
        }
    } finally { $archive.Dispose() }
} finally { $stream.Dispose() }

Write-Host "Package stage: $stageRoot"
Write-Host "Package zip:   $zipPath"
Write-Host "Package SHA256: $((Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant())"

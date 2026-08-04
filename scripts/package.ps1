[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [string]$Version = 'dev',

    [string]$OutputDirectory = 'artifacts',

    [string]$RayTraceAssetsDirectory,

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
$thirdPartyNotices = Join-Path $repoRoot 'packaging/THIRD_PARTY_NOTICES.md'
$rayTraceLicense = Join-Path $repoRoot 'packaging/licenses/RayTrace-GPL-3.0.txt'
$outputRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDirectory))
$stageRoot = Join-Path $outputRoot "stage/HeroShift-$Version"
$zipPath = Join-Path $outputRoot "HeroShift-$Version.zip"
$manifestPath = Join-Path $stageRoot 'package-manifest.json'

$rayTraceVersion = 'build-f483aba'
$rayTraceBaseUrl = "https://git.miksen.me/mikkel/Ray-Trace/releases/download/$rayTraceVersion"
$rayTraceArchives = [ordered]@{
    "RayTrace-CSS-API-$rayTraceVersion.tar.gz" = [ordered]@{
        url = "$rayTraceBaseUrl/RayTrace-CSS-API-$rayTraceVersion.tar.gz"
        sha256 = '1258facfc53d1b37a4cf73047450ceb1a4c1a2846fe7790f3c934d03f3890500'
    }
    "RayTrace-MM-$rayTraceVersion-linux.tar.gz" = [ordered]@{
        url = "$rayTraceBaseUrl/RayTrace-MM-$rayTraceVersion-linux.tar.gz"
        sha256 = 'a025e3202d1ccc52dc681f43524bb2c06d2d7dc6f7471fdc7fdcb397b088746e'
    }
}

function Assert-FileHash {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$ExpectedSha256
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required file is missing: $Path"
    }

    $actual = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne $ExpectedSha256) {
        throw "SHA256 mismatch for $Path. Expected $ExpectedSha256, got $actual"
    }
}

function Copy-TreeFiles {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    if (-not (Test-Path -LiteralPath $Source -PathType Container)) {
        throw "Required package directory is missing: $Source"
    }

    foreach ($file in Get-ChildItem -LiteralPath $Source -File -Recurse | Sort-Object FullName) {
        if ($file.Extension -in @('.pdb', '.xml')) { continue }

        $relativePath = [System.IO.Path]::GetRelativePath($Source, $file.FullName)
        $target = Join-Path $Destination $relativePath
        New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $target -Force
    }
}

if (-not $NoBuild) {
    $buildVersion = if ($Version -match '^v?(\d+\.\d+\.\d+)$') {
        $Matches[1]
    } else {
        '0.0.0-' + ($Version.ToLowerInvariant() -replace '[^0-9a-z.-]', '.')
    }
    & dotnet build $solution -c $Configuration "-p:Version=$buildVersion"
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE" }
}

$requiredSources = [ordered]@{
    'addons/counterstrikesharp/plugins/HeroShift/HeroShift.dll' = Join-Path $buildOutput 'HeroShift.dll'
    'addons/counterstrikesharp/plugins/HeroShift/WASDMenuAPI.dll' = Join-Path $buildOutput 'WASDMenuAPI.dll'
    'addons/counterstrikesharp/plugins/HeroShift/Newtonsoft.Json.dll' = Join-Path $buildOutput 'Newtonsoft.Json.dll'
    'addons/counterstrikesharp/plugins/HeroShift/configs/heroshift.json' = $defaultConfig
    'addons/counterstrikesharp/gamedata/HeroShift.gamedata.json' = $gamedata
    'THIRD_PARTY_NOTICES.md' = $thirdPartyNotices
    'licenses/RayTrace-GPL-3.0.txt' = $rayTraceLicense
}

foreach ($source in $requiredSources.Values) {
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Required package source is missing: $source"
    }
}

New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
if (Test-Path -LiteralPath $stageRoot) {
    Remove-Item -LiteralPath $stageRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $stageRoot -Force | Out-Null

$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("heroshift-package-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temporaryRoot -Force | Out-Null

try {
    $archivePaths = @{}
    foreach ($archive in $rayTraceArchives.GetEnumerator()) {
        $archivePath = if ([string]::IsNullOrWhiteSpace($RayTraceAssetsDirectory)) {
            Join-Path $temporaryRoot $archive.Key
        } else {
            Join-Path ([System.IO.Path]::GetFullPath($RayTraceAssetsDirectory)) $archive.Key
        }

        if ([string]::IsNullOrWhiteSpace($RayTraceAssetsDirectory)) {
            Write-Host "Downloading pinned RayTrace asset $($archive.Key)"
            Invoke-WebRequest -Uri $archive.Value.url -OutFile $archivePath
        }

        Assert-FileHash -Path $archivePath -ExpectedSha256 $archive.Value.sha256
        $archivePaths[$archive.Key] = $archivePath
    }

    $cssExtract = Join-Path $temporaryRoot 'raytrace-css'
    $mmExtract = Join-Path $temporaryRoot 'raytrace-mm'
    New-Item -ItemType Directory -Path $cssExtract, $mmExtract -Force | Out-Null

    & tar -xzf $archivePaths["RayTrace-CSS-API-$rayTraceVersion.tar.gz"] -C $cssExtract
    if ($LASTEXITCODE -ne 0) { throw "Failed to extract the RayTrace CSS archive with exit code $LASTEXITCODE" }
    & tar -xzf $archivePaths["RayTrace-MM-$rayTraceVersion-linux.tar.gz"] -C $mmExtract
    if ($LASTEXITCODE -ne 0) { throw "Failed to extract the RayTrace Metamod archive with exit code $LASTEXITCODE" }

    $cssSource = Join-Path $cssExtract "RayTrace-CSS-API-$rayTraceVersion/counterstrikesharp"
    Copy-TreeFiles -Source $cssSource -Destination (Join-Path $stageRoot 'addons/counterstrikesharp')
    Copy-TreeFiles -Source $mmExtract -Destination (Join-Path $stageRoot 'addons')

    foreach ($entry in $requiredSources.GetEnumerator()) {
        $destination = Join-Path $stageRoot ($entry.Key.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
        New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
        Copy-Item -LiteralPath $entry.Value -Destination $destination -Force
    }

    $requiredRuntimePaths = @($requiredSources.Keys) + @(
        'addons/counterstrikesharp/shared/RayTraceApi/RayTraceApi.dll'
        'addons/counterstrikesharp/plugins/RayTraceImpl/RayTraceImpl.dll'
        'addons/metamod/RayTrace.vdf'
        'addons/RayTrace/gamedata.json'
        'addons/RayTrace/bin/linuxsteamrt64/RayTrace.so'
    )

    foreach ($relativePath in $requiredRuntimePaths) {
        $filePath = Join-Path $stageRoot ($relativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
        if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) {
            throw "Required runtime dependency is missing from the package: $relativePath"
        }
    }

    $packagedFiles = Get-ChildItem -LiteralPath $stageRoot -File -Recurse | ForEach-Object {
        [System.IO.Path]::GetRelativePath($stageRoot, $_.FullName).Replace([System.IO.Path]::DirectorySeparatorChar, '/')
    } | Sort-Object

    if ($packagedFiles | Where-Object { $_ -match '\.(pdb|xml)$' }) {
        throw 'Debug or documentation files leaked into the package.'
    }

    $manifestEntries = @()
    foreach ($relativePath in $packagedFiles) {
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
        bundledDependencies = @(
            [ordered]@{
                name = 'RayTrace'
                version = $rayTraceVersion
                source = 'https://git.miksen.me/mikkel/Ray-Trace/releases'
                archives = @($rayTraceArchives.GetEnumerator() | ForEach-Object {
                    [ordered]@{ name = $_.Key; sha256 = $_.Value.sha256 }
                })
            }
        )
    }
    $manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding utf8NoBOM

    $actualFiles = Get-ChildItem -LiteralPath $stageRoot -File -Recurse | ForEach-Object {
        [System.IO.Path]::GetRelativePath($stageRoot, $_.FullName).Replace([System.IO.Path]::DirectorySeparatorChar, '/')
    } | Sort-Object
    $manifestInventory = @($manifest.files.path) | Sort-Object
    $actualInventory = @($actualFiles | Where-Object { $_ -ne 'package-manifest.json' }) | Sort-Object
    if (Compare-Object -ReferenceObject $manifestInventory -DifferenceObject $actualInventory) {
        throw 'Package manifest inventory does not match the staged runtime files.'
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
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}

Write-Host "Package stage: $stageRoot"
Write-Host "Package zip:   $zipPath"
Write-Host "Package SHA256: $((Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant())"

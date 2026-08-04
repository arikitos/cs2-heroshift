<#
.SYNOPSIS
    Builds, packages, and publishes a HeroShift release.

.DESCRIPTION
    Creates HeroShift-vX.Y.Z.zip in the repository root. Temporary downloads,
    extraction, and staging are kept outside the repository. By default the
    script tags the current main commit, pushes the tag, and creates a GitHub
    release with the generated archive attached.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [string]$RayTraceAssetsDirectory,

    [switch]$NoBuild,

    [switch]$NoPublish
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = $PSScriptRoot
$solution = Join-Path $repoRoot 'HeroShift.sln'
$targetFramework = 'net10.0'
$buildOutput = Join-Path $repoRoot "src/HeroShift/bin/$Configuration/$targetFramework"
$gamedata = Join-Path $repoRoot 'src/HeroShift/Gamedata/HeroShift.gamedata.json'
$defaultConfig = Join-Path $repoRoot 'config/heroshift.json'
$thirdPartyNotices = Join-Path $repoRoot 'THIRD_PARTY_NOTICES.md'
$rayTraceLicense = Join-Path $repoRoot 'licenses/RayTrace-GPL-3.0.txt'
$tag = "v$Version"
$zipPath = Join-Path $repoRoot "HeroShift-$tag.zip"

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

function Invoke-Checked {
    param(
        [Parameter(Mandatory = $true)][string]$Command,
        [Parameter()][string[]]$Arguments = @()
    )

    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Command failed with exit code $LASTEXITCODE"
    }
}

function Assert-Command {
    param([Parameter(Mandatory = $true)][string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command is not available: $Name"
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
        if ($file.Extension -in @('.pdb', '.xml')) {
            continue
        }

        $relativePath = [System.IO.Path]::GetRelativePath($Source, $file.FullName)
        $target = Join-Path $Destination $relativePath
        New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $target -Force
    }
}

function Get-PublishContext {
    Assert-Command -Name 'git'
    Assert-Command -Name 'gh'

    $status = (& git -C $repoRoot status --porcelain)
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to inspect the Git working tree.'
    }
    if ($status) {
        throw 'The Git working tree must be clean before publishing a release.'
    }

    $branch = (& git -C $repoRoot branch --show-current).Trim()
    if ($LASTEXITCODE -ne 0 -or $branch -ne 'main') {
        throw 'Releases must be published from the main branch.'
    }

    Invoke-Checked -Command 'git' -Arguments @('-C', $repoRoot, 'fetch', 'origin', 'main', '--tags')

    $head = (& git -C $repoRoot rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to resolve the current commit.'
    }
    $remoteMain = (& git -C $repoRoot rev-parse origin/main).Trim()
    if ($LASTEXITCODE -ne 0 -or $head -ne $remoteMain) {
        throw 'Local main must exactly match origin main before publishing.'
    }

    & gh auth status | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw 'GitHub CLI is not authenticated. Run gh auth login first.'
    }

    $repository = (& gh repo view --json nameWithOwner --jq '.nameWithOwner').Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repository)) {
        throw 'Unable to resolve the GitHub repository.'
    }

    & git -C $repoRoot show-ref --verify --quiet "refs/tags/$tag"
    if ($LASTEXITCODE -eq 0) {
        throw "Local tag already exists: $tag"
    }

    & git -C $repoRoot ls-remote --exit-code --tags origin "refs/tags/$tag" | Out-Null
    if ($LASTEXITCODE -eq 0) {
        throw "Remote tag already exists: $tag"
    }

    & gh release view $tag --repo $repository 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) {
        throw "GitHub release already exists: $tag"
    }

    return [ordered]@{
        Head = $head
        Repository = $repository
    }
}

$publishContext = if ($NoPublish) { $null } else { Get-PublishContext }

Assert-Command -Name 'tar'
if (-not $NoBuild) {
    Assert-Command -Name 'dotnet'
    Invoke-Checked -Command 'dotnet' -Arguments @('restore', $solution)
    Invoke-Checked -Command 'dotnet' -Arguments @('build', $solution, '-c', $Configuration, '--no-restore', "-p:Version=$Version")
    Invoke-Checked -Command 'dotnet' -Arguments @('test', $solution, '-c', $Configuration, '--no-restore', '--no-build')
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

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("heroshift-release-" + [Guid]::NewGuid().ToString('N'))
$stageRoot = Join-Path $temporaryRoot 'stage'
$manifestPath = Join-Path $stageRoot 'package-manifest.json'
New-Item -ItemType Directory -Path $stageRoot -Force | Out-Null

try {
    $archivePaths = @{}
    foreach ($archive in $rayTraceArchives.GetEnumerator()) {
        $archivePath = if ([string]::IsNullOrWhiteSpace($RayTraceAssetsDirectory)) {
            Join-Path $temporaryRoot $archive.Key
        }
        else {
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

    Invoke-Checked -Command 'tar' -Arguments @('-xzf', $archivePaths["RayTrace-CSS-API-$rayTraceVersion.tar.gz"], '-C', $cssExtract)
    Invoke-Checked -Command 'tar' -Arguments @('-xzf', $archivePaths["RayTrace-MM-$rayTraceVersion-linux.tar.gz"], '-C', $mmExtract)

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

    $manifestEntries = foreach ($relativePath in $packagedFiles) {
        $filePath = Join-Path $stageRoot ($relativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
        $item = Get-Item -LiteralPath $filePath
        [ordered]@{
            path = $relativePath
            size = $item.Length
            sha256 = (Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    }

    $manifest = [ordered]@{
        schemaVersion = 1
        package = 'HeroShift'
        version = $tag
        targetFramework = $targetFramework
        files = @($manifestEntries)
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
                    try {
                        $sourceStream.CopyTo($entryStream)
                    }
                    finally {
                        $sourceStream.Dispose()
                    }
                }
                finally {
                    $entryStream.Dispose()
                }
            }
        }
        finally {
            $archive.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}

$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Host "Release archive: $zipPath"
Write-Host "Release SHA256: $zipHash"

if ($NoPublish) {
    Write-Host 'GitHub publishing was skipped.'
    return
}

$tagCreated = $false
try {
    Invoke-Checked -Command 'git' -Arguments @('-C', $repoRoot, 'tag', '--annotate', $tag, '--message', "HeroShift $tag", $publishContext.Head)
    $tagCreated = $true
    Invoke-Checked -Command 'git' -Arguments @('-C', $repoRoot, 'push', 'origin', "refs/tags/$tag")

    & gh release create $tag $zipPath `
        --repo $publishContext.Repository `
        --verify-tag `
        --title "HeroShift $tag" `
        --generate-notes `
        --latest
    if ($LASTEXITCODE -ne 0) {
        throw "GitHub release creation failed with exit code $LASTEXITCODE"
    }
}
catch {
    if ($tagCreated) {
        & gh release delete $tag --repo $publishContext.Repository --yes 2>$null | Out-Null
        & git -C $repoRoot push origin ":refs/tags/$tag" 2>$null | Out-Null
        & git -C $repoRoot tag --delete $tag 2>$null | Out-Null
    }
    throw
}

Write-Host "Published GitHub release: $tag"

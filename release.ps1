<#
.SYNOPSIS
    Cuts a HeroShift release: bumps the version, builds, zips the server files,
    tags the commit and pushes the tag.

.DESCRIPTION
    Run from the repository root. Requires git and the .NET 10 SDK.

    Every precondition is checked before anything is modified, so a failed run
    never leaves a half-applied version bump behind.

    Pushing the tag triggers .github/workflows/release.yml, which rebuilds and
    publishes the GitHub Release with the packaged zip attached. If that workflow
    is absent or disabled, create the release manually and upload the local zip
    this script leaves in the repository root.

.PARAMETER Version
    The release version without the leading "v" (e.g. 1.0.0).

.PARAMETER NoPush
    Create the commit and tag locally but do not push. Nothing leaves the machine.

.EXAMPLE
    ./release.ps1 -Version 1.0.0

.EXAMPLE
    ./release.ps1 -Version 1.0.0 -NoPush
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,

    [switch]$NoPush
)

$ErrorActionPreference = 'Stop'

$repoRoot   = $PSScriptRoot
$sln        = Join-Path $repoRoot 'HeroShift - SRC Files/HeroShift.sln'
$sourceFile = Join-Path $repoRoot 'HeroShift - SRC Files/src/HeroShift.cs'
$serverDir  = Join-Path $repoRoot 'HeroShift - Server Files'
$zipPath    = Join-Path $repoRoot "HeroShift-v$Version.zip"
$tag        = "v$Version"

# Locates a dotnet with an SDK (not just a runtime). The machine-wide install is
# often runtime-only, so also probe DOTNET_ROOT and the user-local install dir
# that dotnet-install.ps1 uses by default.
function Resolve-DotNetWithSdk {
    $candidates = New-Object System.Collections.Generic.List[string]

    $onPath = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($onPath) { $candidates.Add($onPath.Source) }
    if ($env:DOTNET_ROOT) { $candidates.Add((Join-Path $env:DOTNET_ROOT 'dotnet.exe')) }
    if ($env:LOCALAPPDATA) { $candidates.Add((Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe')) }
    $candidates.Add('C:\Program Files\dotnet\dotnet.exe')

    foreach ($candidate in $candidates) {
        if (-not $candidate -or -not (Test-Path -LiteralPath $candidate)) { continue }
        $sdks = @()
        try { $sdks = @(& $candidate --list-sdks) } catch { $sdks = @() }
        if ($sdks.Count -gt 0) { return $candidate }
    }
    return $null
}

# ---------------------------------------------------------------------------
# Preflight - nothing below this block is modified until every check passes.
# ---------------------------------------------------------------------------
Write-Host 'Running preflight checks...' -ForegroundColor Cyan

foreach ($required in @($sln, $sourceFile)) {
    if (-not (Test-Path -LiteralPath $required)) { throw "Missing required path: $required" }
}
if (-not (Test-Path -LiteralPath $serverDir)) { throw "Missing required path: $serverDir" }

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw 'git was not found on PATH.'
}

# Refuse to release with a dirty working tree, so the release commit contains
# only the version bump and the rebuilt server files.
$status = git status --porcelain
if ($status) {
    throw "Working tree is not clean. Commit or stash changes before releasing.`n$status"
}

if ((git tag --list $tag)) {
    throw "Tag $tag already exists. Delete it or pick another version."
}

$dotnet = Resolve-DotNetWithSdk
if (-not $dotnet) {
    throw @'
No .NET SDK found (a runtime-only install cannot build).
Install the .NET 10 SDK from https://dotnet.microsoft.com/download,
or set DOTNET_ROOT to an existing SDK install.
'@
}
Write-Host "  dotnet: $dotnet" -ForegroundColor DarkGray

# Confirm the bump target exists before touching the file.
$versionPattern = 'ModuleVersion\s*=>\s*"[^"]*"'
$originalBytes = [System.IO.File]::ReadAllBytes($sourceFile)
$content = [System.IO.File]::ReadAllText($sourceFile)
if ($content -notmatch $versionPattern) {
    throw "Could not find ModuleVersion in $sourceFile"
}
Write-Host '  preflight OK' -ForegroundColor DarkGray

# ---------------------------------------------------------------------------
# 1. Bump ModuleVersion in the source.
# ---------------------------------------------------------------------------
Write-Host "Bumping ModuleVersion to $Version..." -ForegroundColor Cyan

# The source is UTF-8 and contains non-ASCII characters, so write it back with an
# explicit UTF-8 encoder preserving the original BOM. Set-Content would default
# to the ANSI codepage under Windows PowerShell and corrupt them.
$hasBom = $originalBytes.Length -ge 3 -and
          $originalBytes[0] -eq 0xEF -and $originalBytes[1] -eq 0xBB -and $originalBytes[2] -eq 0xBF
$updated = [regex]::Replace($content, $versionPattern, "ModuleVersion => `"$Version`"")
[System.IO.File]::WriteAllText($sourceFile, $updated, (New-Object System.Text.UTF8Encoding($hasBom)))

# ---------------------------------------------------------------------------
# 2. Build in Release (copies DLLs, languages and gamedata into the server folder).
# ---------------------------------------------------------------------------
Write-Host 'Building (Release)...' -ForegroundColor Cyan
& $dotnet build $sln -c Release
if ($LASTEXITCODE -ne 0) {
    # Undo the bump so a failed build leaves the tree exactly as it was.
    [System.IO.File]::WriteAllBytes($sourceFile, $originalBytes)
    throw "Build failed (exit $LASTEXITCODE). Reverted the version bump."
}

# ---------------------------------------------------------------------------
# 3. Package the server files.
# ---------------------------------------------------------------------------
Write-Host "Packaging $zipPath..." -ForegroundColor Cyan
if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }

# Stage a copy so debug symbols can be excluded without disturbing the build output.
$staging = Join-Path ([System.IO.Path]::GetTempPath()) "HeroShift-release-$Version"
if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
New-Item -ItemType Directory -Path $staging | Out-Null
try {
    Copy-Item -Path (Join-Path $serverDir '*') -Destination $staging -Recurse -Force
    Get-ChildItem -Path $staging -Recurse -Filter '*.pdb' | Remove-Item -Force
    Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $zipPath -Force
} finally {
    Remove-Item -LiteralPath $staging -Recurse -Force -ErrorAction SilentlyContinue
}

# ---------------------------------------------------------------------------
# 4. Commit the version bump and tag it.
# ---------------------------------------------------------------------------
Write-Host "Committing and tagging $tag..." -ForegroundColor Cyan
git add -- 'HeroShift - SRC Files/src/HeroShift.cs' 'HeroShift - Server Files'
if ($LASTEXITCODE -ne 0) { throw "git add failed (exit $LASTEXITCODE)." }
git commit -m "Release $tag"
if ($LASTEXITCODE -ne 0) { throw "git commit failed (exit $LASTEXITCODE)." }
git tag -a $tag -m "HeroShift $tag"
if ($LASTEXITCODE -ne 0) { throw "git tag failed (exit $LASTEXITCODE)." }

# ---------------------------------------------------------------------------
# 5. Push (unless -NoPush).
# ---------------------------------------------------------------------------
if ($NoPush) {
    Write-Host "Created commit and tag $tag locally. Skipping push (-NoPush)." -ForegroundColor Yellow
    Write-Host "Push manually with: git push origin HEAD; git push origin $tag"
} else {
    Write-Host 'Pushing commit and tag...' -ForegroundColor Cyan
    git push origin HEAD
    if ($LASTEXITCODE -ne 0) { throw "git push of the commit failed (exit $LASTEXITCODE)." }
    git push origin $tag
    if ($LASTEXITCODE -ne 0) { throw "git push of the tag failed (exit $LASTEXITCODE)." }
    Write-Host "Done. The Release workflow will build and publish $tag." -ForegroundColor Green
}

Write-Host "Local package: $zipPath" -ForegroundColor Green

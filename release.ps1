<#
.SYNOPSIS
    Builds and packages HeroShift locally without modifying Git history.

.DESCRIPTION
    This wrapper delegates to scripts/package.ps1. It never edits source files,
    commits, tags, or pushes. Create and push a version tag separately when the
    generated archive has been reviewed; the Release workflow will rebuild the
    same package from that tag.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [string]$OutputDirectory = 'artifacts',

    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$packageScript = Join-Path $PSScriptRoot 'scripts/package.ps1'
if (-not (Test-Path -LiteralPath $packageScript -PathType Leaf)) {
    throw "Packaging script is missing: $packageScript"
}

& $packageScript `
    -Configuration $Configuration `
    -Version "v$Version" `
    -OutputDirectory $OutputDirectory `
    -NoBuild:$NoBuild

if ($LASTEXITCODE -ne 0) {
    throw "Packaging failed with exit code $LASTEXITCODE"
}

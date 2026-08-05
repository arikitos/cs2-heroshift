$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$python = Get-Command python -ErrorAction SilentlyContinue
if (-not $python) {
    $python = Get-Command py -ErrorAction SilentlyContinue
}
if (-not $python) {
    throw 'Python 3 is required to run HeroEditor.'
}

$server = Join-Path $PSScriptRoot 'server.py'
if ($python.Name -eq 'py.exe' -or $python.Name -eq 'py') {
    & $python.Source -3 $server
}
else {
    & $python.Source $server
}

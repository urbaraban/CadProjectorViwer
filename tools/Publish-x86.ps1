# Publish 32-bit (win-x86) framework-dependent build of 2CUT-Viewer.
# Requires .NET 10 Desktop Runtime (x86) on target machines.
param(
    [string]$OutputDir = "",
    [switch]$SelfContained
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "MonchaCadViewer\CadProjectorViewer.csproj"

if (-not $OutputDir) {
    $OutputDir = Join-Path $root "artifacts\win-x86"
}

$rid = "win-x86"
$sc = if ($SelfContained) { "true" } else { "false" }

Write-Host "Publishing CadProjectorViewer ($rid), SelfContained=$sc"
Write-Host "Output: $OutputDir"

dotnet publish $project `
    -c Release `
    -r $rid `
    --self-contained $sc `
    -o $OutputDir `
    /p:Platform=x86

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$exe = Join-Path $OutputDir "2CUT-Viewer.exe"
if (-not (Test-Path $exe)) {
    throw "Expected output not found: $exe"
}

Write-Host "Done: $exe"

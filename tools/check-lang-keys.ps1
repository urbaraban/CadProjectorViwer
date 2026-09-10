#!/usr/bin/env pwsh
# Fail if ru-RU and en-US language keys diverge.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$ru = Join-Path $root "src/CadProjector.App/Assets/Lang/ru-RU.axaml"
$en = Join-Path $root "src/CadProjector.App/Assets/Lang/en-US.axaml"

function Get-Keys([string]$path) {
    Select-String -Path $path -Pattern 'x:Key="([^"]+)"' | ForEach-Object { $_.Matches[0].Groups[1].Value } | Sort-Object -Unique
}

$ruKeys = Get-Keys $ru
$enKeys = Get-Keys $en
$onlyRu = Compare-Object $ruKeys $enKeys | Where-Object SideIndicator -eq "<=" | ForEach-Object InputObject
$onlyEn = Compare-Object $ruKeys $enKeys | Where-Object SideIndicator -eq "=>" | ForEach-Object InputObject

if ($onlyRu -or $onlyEn) {
    Write-Host "Language key mismatch:"
    if ($onlyRu) { Write-Host "  only in ru-RU: $($onlyRu -join ', ')" }
    if ($onlyEn) { Write-Host "  only in en-US: $($onlyEn -join ', ')" }
    exit 1
}

Write-Host "OK: $($ruKeys.Count) keys match in ru-RU and en-US"

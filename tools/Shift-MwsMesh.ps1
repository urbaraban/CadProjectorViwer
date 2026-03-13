param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$InputPath,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$MeshGuid,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$DeltaX,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$DeltaY,

    [string]$OutputPath,

    [switch]$NoBackup
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Normalize-GuidString {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return $Value.Trim().Trim("{", "}").ToLowerInvariant()
}

function Parse-InvariantDouble {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return [double]::Parse($Value, [System.Globalization.CultureInfo]::InvariantCulture)
}

function Parse-UserDouble {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value,
        [Parameter(Mandatory = $true)]
        [string]$ParameterName
    )

    $normalized = $Value.Trim().Replace(",", ".")
    try {
        return Parse-InvariantDouble -Value $normalized
    }
    catch {
        throw "Invalid value for '$ParameterName': '$Value'. Use numeric format like 0.01 or 0,01."
    }
}

function Format-InvariantDouble {
    param(
        [Parameter(Mandatory = $true)]
        [double]$Value
    )

    return $Value.ToString("G17", [System.Globalization.CultureInfo]::InvariantCulture)
}

if (-not (Test-Path -LiteralPath $InputPath)) {
    throw "Input file not found: $InputPath"
}

$sourcePath = (Resolve-Path -LiteralPath $InputPath).Path
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = $sourcePath
}

$deltaXValue = Parse-UserDouble -Value $DeltaX -ParameterName "DeltaX"
$deltaYValue = Parse-UserDouble -Value $DeltaY -ParameterName "DeltaY"

$targetGuid = Normalize-GuidString -Value $MeshGuid
[xml]$xml = Get-Content -LiteralPath $sourcePath -Raw -Encoding UTF8

$meshNodes = $xml.SelectNodes("//Device/Meshes/Mesh")
if ($null -eq $meshNodes -or $meshNodes.Count -eq 0) {
    throw "No Mesh nodes were found under Device in input file."
}

$targetMeshes = @()
foreach ($mesh in $meshNodes) {
    $uid = $mesh.GetAttribute("Uid")
    if (-not [string]::IsNullOrWhiteSpace($uid)) {
        if ((Normalize-GuidString -Value $uid) -eq $targetGuid) {
            $targetMeshes += $mesh
        }
    }
}

if ($targetMeshes.Count -eq 0) {
    throw "Mesh with guid '$MeshGuid' was not found."
}

$totalPoints = 0
$meshCount = 0

foreach ($mesh in $targetMeshes) {
    $meshCount++
    $points = $mesh.SelectNodes("./Point")
    foreach ($point in $points) {
        $oldXText = $point.GetAttribute("X")
        $oldYText = $point.GetAttribute("Y")

        if ([string]::IsNullOrWhiteSpace($oldXText) -or [string]::IsNullOrWhiteSpace($oldYText)) {
            continue
        }

        $oldX = Parse-InvariantDouble -Value $oldXText
        $oldY = Parse-InvariantDouble -Value $oldYText

        $newX = $oldX + $deltaXValue
        $newY = $oldY + $deltaYValue

        $point.SetAttribute("X", (Format-InvariantDouble -Value $newX))
        $point.SetAttribute("Y", (Format-InvariantDouble -Value $newY))
        $totalPoints++
    }
}

$targetPath = $OutputPath
if (Test-Path -LiteralPath $targetPath) {
    $targetPath = (Resolve-Path -LiteralPath $targetPath).Path
}

if (($targetPath -ieq $sourcePath) -and (-not $NoBackup)) {
    $backupPath = "$sourcePath.bak"
    Copy-Item -LiteralPath $sourcePath -Destination $backupPath -Force
    Write-Host "Backup created: $backupPath"
}

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$settings = New-Object System.Xml.XmlWriterSettings
$settings.Encoding = $utf8NoBom
$settings.Indent = $true
$settings.NewLineChars = "`r`n"
$settings.NewLineHandling = [System.Xml.NewLineHandling]::Replace

$writer = [System.Xml.XmlWriter]::Create($OutputPath, $settings)
try {
    $xml.Save($writer)
}
finally {
    if ($null -ne $writer) {
        $writer.Dispose()
    }
}

Write-Host "Done. Meshes changed: $meshCount, points changed: $totalPoints."
Write-Host "Shift applied: DeltaX=$deltaXValue, DeltaY=$deltaYValue"
Write-Host "Saved to: $OutputPath"

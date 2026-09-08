# UDP smoke (legacy Filename + CLEAR&PLAY)
# Usage: .\udp-smoke.ps1 -Port 11000 -File "C:\path\to\sample.svg" -WorkFolder "C:\folder"
param(
  [int]$Port = 11000,
  [string]$File = "",
  [string]$WorkFolder = (Get-Location).Path,
  [ValidateSet("Filename","Filepath")]
  [string]$Mode = "Filepath",
  [string]$Commands = "CLEAR&PLAY"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net

function Write-AsciiPadded([string]$s, [int]$len) {
  $bytes = [System.Text.Encoding]::ASCII.GetBytes($s.PadRight($len).Substring(0, $len))
  return $bytes
}

$pathOrName = if ($Mode -eq "Filename") { Split-Path $File -Leaf } else { $File }
if ([string]::IsNullOrWhiteSpace($pathOrName) -and $Mode -eq "Filepath") {
  Write-Host "Provide -File for Filepath, or -Mode Filename with a file under WorkFolder"
  exit 1
}

$cmdBytes = [System.Text.Encoding]::ASCII.GetBytes($Commands)
$enc = [System.Text.Encoding]::GetEncoding(1251)
$nameBytes = $enc.GetBytes($pathOrName)

$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter $ms
$bw.Write((Write-AsciiPadded $Mode 8))
$bw.Write([int32]1)          # TaskId
$bw.Write([int16]0)          # TableId
$bw.Write([int16]$cmdBytes.Length)
$bw.Write($cmdBytes)
$bw.Write([int16]$nameBytes.Length)
$bw.Write($nameBytes)
$bw.Flush()
$packet = $ms.ToArray()

$udp = New-Object System.Net.Sockets.UdpClient
$udp.Connect("127.0.0.1", $Port)
[void]$udp.Send($packet, $packet.Length)
$udp.Close()

Write-Host "Sent $Mode $($packet.Length) bytes to 127.0.0.1:$Port cmds=$Commands path=$pathOrName"
if ($Mode -eq "Filename") { Write-Host "Ensure app WorkFolder = $WorkFolder" }

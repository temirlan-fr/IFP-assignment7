$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$artifacts = Join-Path $root 'artifacts'
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
$zip = Join-Path $artifacts 'IFP-assignment7.zip'
& git -C $root archive --format=zip --output=$zip HEAD
if ($LASTEXITCODE -ne 0) { throw 'Packaging committed source failed.' }
Write-Output "Submission archive created at $zip"

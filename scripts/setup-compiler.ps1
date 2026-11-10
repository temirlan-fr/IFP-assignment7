$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$toolsDirectory = Join-Path $root '.tools'
$compilerDirectory = Join-Path $toolsDirectory 'zig-x86_64-windows-0.14.1'
$compiler = Join-Path $compilerDirectory 'zig.exe'
if (Test-Path -LiteralPath $compiler) {
    & $compiler version
    if ($LASTEXITCODE -ne 0) { throw 'Compiler verification failed.' }
    exit 0
}
New-Item -ItemType Directory -Force -Path $toolsDirectory | Out-Null
$archive = Join-Path $toolsDirectory 'zig-0.14.1.zip'
& curl.exe --fail --location --retry 3 --output $archive 'https://ziglang.org/download/0.14.1/zig-x86_64-windows-0.14.1.zip'
if ($LASTEXITCODE -ne 0) { throw 'Compiler download failed.' }
$expected = '554f5378228923ffd558eac35e21af020c73789d87afeabf4bfd16f2e6feed2c'
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expected) {
    throw 'Compiler archive checksum mismatch.'
}
Expand-Archive -LiteralPath $archive -DestinationPath $toolsDirectory -Force
& $compiler version
if ($LASTEXITCODE -ne 0) { throw 'Compiler verification failed.' }

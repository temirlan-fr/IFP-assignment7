param([string]$Compiler)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (-not $Compiler) { $Compiler = Join-Path $root '.tools/zig-x86_64-windows-0.14.1/zig.exe' }
if (-not (Test-Path -LiteralPath $Compiler)) { throw 'Run scripts/setup-compiler.ps1 first or pass the full Zig executable path with -Compiler.' }
$outputDirectory = Join-Path $root 'native/build/win-x64'
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$env:ZIG_GLOBAL_CACHE_DIR = Join-Path $root '.tools/zig-cache'
& $Compiler cc -std=c11 -Wall -Wextra -Werror -O2 -shared -target x86_64-windows-gnu (Join-Path $root 'native/inventory_native.c') -o (Join-Path $outputDirectory 'inventory_native.dll')
if ($LASTEXITCODE -ne 0) { throw 'Native compilation failed.' }
& $Compiler cc -std=c11 -Wall -Wextra -Werror -O2 -target x86_64-windows-gnu (Join-Path $root 'native/inventory_native.c') (Join-Path $root 'native/native_tests.c') -o (Join-Path $outputDirectory 'native_tests.exe')
if ($LASTEXITCODE -ne 0) { throw 'Native test compilation failed.' }
& (Join-Path $outputDirectory 'native_tests.exe')
if ($LASTEXITCODE -ne 0) { throw 'Native assertions failed.' }
Write-Output "Native DLL built at $outputDirectory/inventory_native.dll"

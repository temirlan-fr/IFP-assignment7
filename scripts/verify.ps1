param([string]$Compiler)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    & (Join-Path $PSScriptRoot 'build-native.ps1') -Compiler $Compiler
    if ($LASTEXITCODE -ne 0) { throw 'Native build failed.' }
    & dotnet build tests/InventoryApp.Tests/InventoryApp.Tests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Managed build failed.' }
    & dotnet run --no-build -c Release --project tests/InventoryApp.Tests
    if ($LASTEXITCODE -ne 0) { throw 'Managed tests failed.' }
    & dotnet run --no-build -c Release --project src/InventoryApp
    if ($LASTEXITCODE -ne 0) { throw 'Application run failed.' }
}
finally { Pop-Location }

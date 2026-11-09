# Build OPoptimizer on Windows 10/11 with .NET 8 SDK installed.
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'Falta instalar .NET 8 SDK desde https://dotnet.microsoft.com/download/dotnet/8.0'
}

Write-Host 'Compilando OPoptimizer (Windows x64)...' -ForegroundColor Cyan
dotnet publish .\OPoptimizer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\dist
if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación.' }

Write-Host '¡Listo! El programa está en dist\OPoptimizer.exe' -ForegroundColor Green

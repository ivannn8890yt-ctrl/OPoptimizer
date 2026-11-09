@echo off
setlocal EnableExtensions
chcp 65001 >nul 2>&1
title OPoptimizer - Compilador de 1 clic
color 09
cd /d "%~dp0"

 echo.
 echo  =============================================
 echo          OPoptimizer - COMPILADOR
 echo  =============================================
 echo.

if not exist "OPoptimizer.csproj" (
  echo [ERROR] No encuentro OPoptimizer.csproj.
  echo Extrae TODO el ZIP y ejecuta este archivo desde la carpeta del proyecto.
  goto :error
)

set "DOTNET=dotnet"
if exist "%ProgramFiles%\dotnet\dotnet.exe" set "DOTNET=%ProgramFiles%\dotnet\dotnet.exe"
call :CheckSDK
if "%SDK_OK%"=="1" goto :build

echo [AVISO] No encontre un SDK de .NET compatible (8 o superior).
echo Se necesita para convertir el codigo C# en un archivo .exe.
echo.
where winget >nul 2>&1
if errorlevel 1 goto :manual

choice /C SN /N /M "Deseas instalar .NET 8 SDK con winget? [S/N]: "
if errorlevel 2 goto :manual

echo.
echo Instalando el SDK oficial de Microsoft. Windows puede pedir confirmacion.
winget install --id Microsoft.DotNet.SDK.8 --exact --source winget --accept-package-agreements --accept-source-agreements
if errorlevel 1 goto :manual
if exist "%ProgramFiles%\dotnet\dotnet.exe" set "DOTNET=%ProgramFiles%\dotnet\dotnet.exe"
call :CheckSDK
if "%SDK_OK%"=="1" goto :build

echo [AVISO] Windows puede necesitar una nueva terminal para reconocer .NET.
echo Cierra esta ventana y ejecuta COMPILAR_OPoptimizer.cmd otra vez.
goto :error

:manual
echo.
echo Descarga e instala .NET 8 SDK desde la pagina oficial:
echo https://dotnet.microsoft.com/download/dotnet/8.0
echo Tras instalarlo, vuelve a hacer doble clic en este archivo.
choice /C SN /N /M "Abrir pagina oficial? [S/N]: "
if errorlevel 2 goto :error
start "" "https://dotnet.microsoft.com/download/dotnet/8.0"
goto :error

:CheckSDK
set "SDK_OK=0"
"%DOTNET%" --version >nul 2>&1
if errorlevel 1 exit /b
for /f "tokens=1 delims=." %%V in ('"%DOTNET%" --version 2^>nul') do (
  if %%V GEQ 8 set "SDK_OK=1"
)
exit /b

:build
echo.
echo [1/2] Compilando para Windows de 64 bits...
echo Se restauraran dependencias oficiales de NuGet si hacen falta.
echo La primera compilacion requiere internet.
echo.
if not exist dist mkdir dist
"%DOTNET%" publish "OPoptimizer.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "dist"
if errorlevel 1 goto :build_error
if not exist "dist\OPoptimizer.exe" goto :build_error

echo.
echo [2/2] COMPILACION FINALIZADA.
echo El programa se encuentra aqui:
echo %CD%\dist\OPoptimizer.exe
echo.
choice /C SN /N /M "Abrir la carpeta del EXE? [S/N]: "
if errorlevel 2 goto :success
start "" "%CD%\dist"
:success
echo.
echo Ya puedes ejecutar OPoptimizer.exe. Revisa el programa antes de distribuirlo.
pause
exit /b 0

:build_error
echo.
echo [ERROR] No se pudo generar el ejecutable.
echo Lee el mensaje de error que aparece arriba para saber que fallo.
echo Revisa tu conexion a internet y que tengas espacio libre.
:error
echo.
pause
exit /b 1

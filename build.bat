@echo off
setlocal EnableDelayedExpansion
title Itchy Backup - Build Script
color 0A

echo.
echo  ==========================================
echo       ITCHY BACKUP BUILD SCRIPT
echo       M.Mert - Itchy Tech
echo  ==========================================
echo.

set PROJECT=src\ItchyBackup\ItchyBackup.csproj
set PUBLISH_SC=build\publish_sc
set PUBLISH_FD=build\publish_fd
set OUTPUT_DIR=build\output
set VERSION=0.5

:: ── Gereksinim kontrolü ──────────────────────────────────────────────────
echo [1/5] Gereksinimler kontrol ediliyor...

where dotnet >nul 2>&1
if errorlevel 1 (
    echo  HATA: .NET SDK bulunamadi!
    echo  Indir: https://dotnet.microsoft.com/download/dotnet/8.0
    pause & exit /b 1
)
for /f "tokens=*" %%v in ('dotnet --version') do set DOTNET_VER=%%v
echo   .NET SDK: !DOTNET_VER! - OK

set "INNO_EXE="
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" set "INNO_EXE=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if exist "C:\Program Files\Inno Setup 6\ISCC.exe" set "INNO_EXE=C:\Program Files\Inno Setup 6\ISCC.exe"
if defined INNO_EXE (echo   Inno Setup: bulundu) else (echo   Inno Setup: yok - sadece portable uretilecek)

:: ── Temizle ──────────────────────────────────────────────────────────────
echo.
echo [2/5] Temizleniyor...
if exist build rmdir /s /q build
mkdir "%PUBLISH_SC%"
mkdir "%PUBLISH_FD%"
mkdir "%OUTPUT_DIR%"
echo   Temizlendi.

:: ── Self-Contained Portable (.exe - Runtime dahil) ───────────────────────
echo.
echo [3/5] Portable .exe derleniyor (Runtime dahil, buyuk boyut)...

dotnet publish "%PROJECT%" ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -p:DebugType=none ^
    -p:DebugSymbols=false ^
    -o "%PUBLISH_SC%"

if errorlevel 1 (
    echo  HATA: Self-contained derleme basarisiz!
    pause & exit /b 1
)

copy "%PUBLISH_SC%\ItchyBackup.exe" "%OUTPUT_DIR%\ItchyBackup_v%VERSION%_portable.exe" >nul
echo   Portable: build\output\ItchyBackup_v%VERSION%_portable.exe - OK

:: ── Framework-Dependent (Setup icin) ─────────────────────────────────────
echo.
echo [4/5] Setup icin derleniyor (kucuk boyut, Runtime gerektirir)...

dotnet publish "%PROJECT%" ^
    -c Release ^
    -r win-x64 ^
    --self-contained false ^
    -p:PublishSingleFile=true ^
    -p:DebugType=none ^
    -p:DebugSymbols=false ^
    -o "%PUBLISH_FD%"

if errorlevel 1 (
    echo  HATA: Framework-dependent derleme basarisiz!
    pause & exit /b 1
)

:: ── Inno Setup ───────────────────────────────────────────────────────────
echo.
echo [5/5] Setup olusturuluyor...

if defined INNO_EXE (
    "%INNO_EXE%" installer\ItchyBackup.iss
    if errorlevel 1 (
        echo   UYARI: Setup olusturulamadi.
    ) else (
        echo   Setup: build\output\ItchyBackup_v%VERSION%_Setup.exe - OK
    )
) else (
    echo   Inno Setup yok, setup atlandi.
)

:: ── Özet ─────────────────────────────────────────────────────────────────
echo.
echo  ==========================================
echo           BUILD TAMAMLANDI!
echo  ==========================================
echo.
echo  Cikti dosyalari: build\output\
dir /b "%OUTPUT_DIR%"
echo.
echo  NOT: portable.exe runtime dahil buyuk dosyadir (~80MB)
echo       Setup.exe daha kucuk olup .NET 8 Runtime gerektirir
echo.
pause

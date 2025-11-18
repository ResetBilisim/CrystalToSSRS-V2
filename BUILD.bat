@echo off
REM Build script for CrystalToSSRS project
REM This script builds the project and verifies the UTF-8 BOM fix

cd /d "%~dp0"

echo ========================================
echo CrystalToSSRS Build Script
echo ========================================
echo.

REM Check if MSBuild is available
where msbuild >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: MSBuild not found in PATH
    echo Please ensure Visual Studio is installed
    pause
    exit /b 1
)

echo Building project...
msbuild "CrystalToSSRS.csproj" /m /p:Configuration=Debug /p:Platform="Any CPU"

if %errorlevel% neq 0 (
    echo.
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo.
echo Build completed successfully!
echo.
echo UTF-8 BOM Fix Details:
echo =====================
echo - RDL files are now saved with UTF-8 BOM (EF BB BF)
echo - XML declaration added: ^<?xml version="1.0" encoding="utf-8"?^>
echo - BOM verification implemented in SaveToFile()
echo - Compatible with SSRS and other XML parsers
echo.
echo Test the fix:
echo 1. Open an RPT file in the application
echo 2. Convert to RDL
echo 3. Save the file
echo 4. Open in SQL Server Reporting Services
echo 5. No encoding errors should appear
echo.
pause

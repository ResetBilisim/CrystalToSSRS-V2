# UTF-8 BOM Verification Script
# This script verifies that RDL files are saved with correct UTF-8 BOM encoding

param(
    [string]$RdlFilePath = "",
    [string]$TestDirectory = "."
)

function Test-UTF8BOM {
    param(
        [string]$FilePath
    )
    
    if (-not (Test-Path $FilePath)) {
        Write-Host "File not found: $FilePath" -ForegroundColor Red
        return $false
    }
    
    $bytes = [System.IO.File]::ReadAllBytes($FilePath)
    
    if ($bytes.Length -lt 3) {
        Write-Host "File too small to contain BOM" -ForegroundColor Yellow
        return $false
    }
    
    $hasBom = ($bytes[0] -eq 0xEF) -and ($bytes[1] -eq 0xBB) -and ($bytes[2] -eq 0xBF)
    
    if ($hasBom) {
        Write-Host "✓ UTF-8 BOM detected (EF BB BF)" -ForegroundColor Green
        return $true
    } else {
        Write-Host "✗ UTF-8 BOM NOT found" -ForegroundColor Red
        Write-Host "  First 3 bytes: $('{0:X2}' -f $bytes[0]) $('{0:X2}' -f $bytes[1]) $('{0:X2}' -f $bytes[2])" -ForegroundColor Yellow
        return $false
    }
}

function Test-XMLDeclaration {
    param(
        [string]$FilePath
    )
    
    $content = Get-Content $FilePath -Raw -Encoding UTF8
    
    if ($content -match '^\s*<\?xml\s+version\s*=\s*["\']1\.0["\'].*encoding\s*=\s*["\']utf-8["\']') {
        Write-Host "✓ XML declaration with UTF-8 encoding found" -ForegroundColor Green
        return $true
    } else {
        Write-Host "✗ XML declaration not found or encoding mismatch" -ForegroundColor Yellow
        return $false
    }
}

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "UTF-8 BOM Verification Tool" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

if ($RdlFilePath) {
    # Test specific file
    Write-Host "Testing: $RdlFilePath" -ForegroundColor White
    Write-Host ""
    
    Write-Host "Checking UTF-8 BOM..." -ForegroundColor Yellow
    $bomResult = Test-UTF8BOM $RdlFilePath
    Write-Host ""
    
    Write-Host "Checking XML Declaration..." -ForegroundColor Yellow
    $xmlResult = Test-XMLDeclaration $RdlFilePath
    Write-Host ""
    
    if ($bomResult -and $xmlResult) {
        Write-Host "RESULT: File is correctly encoded" -ForegroundColor Green
    } else {
        Write-Host "RESULT: File encoding issues detected" -ForegroundColor Red
    }
} else {
    # Test all RDL files in directory
    Write-Host "Scanning for RDL files in: $TestDirectory" -ForegroundColor White
    Write-Host ""
    
    $rdlFiles = Get-ChildItem -Path $TestDirectory -Filter "*.rdl" -Recurse -ErrorAction SilentlyContinue
    
    if ($rdlFiles.Count -eq 0) {
        Write-Host "No RDL files found in: $TestDirectory" -ForegroundColor Yellow
        exit
    }
    
    $passCount = 0
    $failCount = 0
    
    foreach ($file in $rdlFiles) {
        Write-Host "File: $($file.FullName)" -ForegroundColor White
        
        if (Test-UTF8BOM $file.FullName) {
            $passCount++
        } else {
            $failCount++
        }
        
        Write-Host ""
    }
    
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host "Summary:" -ForegroundColor Cyan
    Write-Host "  Total Files: $($rdlFiles.Count)" -ForegroundColor White
    Write-Host "  Correct BOM: $passCount" -ForegroundColor Green
    Write-Host "  Incorrect BOM: $failCount" -ForegroundColor $(if ($failCount -gt 0) { "Red" } else { "Green" })
    Write-Host "======================================" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "For more information, see: UTF8_BOM_FIX_DOCUMENTATION.md" -ForegroundColor Gray

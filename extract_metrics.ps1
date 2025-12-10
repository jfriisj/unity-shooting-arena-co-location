<#
.SYNOPSIS
    Extracts metrics data from connected Meta Quest devices.

.DESCRIPTION
    This script pulls metrics CSV files and metadata from all connected
    Meta Quest devices to a local directory for analysis.

.PARAMETER SessionName
    Optional name for the session folder. Defaults to current date.

.PARAMETER OutputDir
    Output directory for extracted data. Defaults to research-paper/data/sessions

.EXAMPLE
    .\extract_metrics.ps1 -SessionName "experiment_1"
#>

param(
    [string]$SessionName = (Get-Date -Format "yyyyMMdd"),
    [string]$OutputDir = "research-paper/data/sessions"
)

$ErrorActionPreference = "Stop"

# Package name for the app
$PackageName = "com.jJFiisJ.ArenaShooting"

# Find ADB
$AdbPaths = @(
    "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe",
    "$env:USERPROFILE\AppData\Local\Android\Sdk\platform-tools\adb.exe",
    "$env:USERPROFILE\Android\Sdk\platform-tools\adb.exe",
    "C:\Android\Sdk\platform-tools\adb.exe",
    "adb"
)

$Adb = $null
foreach ($path in $AdbPaths) {
    if (Test-Path $path -ErrorAction SilentlyContinue) {
        $Adb = $path
        break
    }
    # Check if it's in PATH
    try {
        $null = & $path version 2>$null
        $Adb = $path
        break
    } catch { }
}

if (-not $Adb) {
    Write-Error "ADB not found. Please install Android SDK platform-tools or add adb to PATH."
    exit 1
}

Write-Host "Using ADB: $Adb" -ForegroundColor Green

# Get connected devices
$devices = & $Adb devices | Select-String -Pattern "^\S+\s+device$" | ForEach-Object { ($_ -split "\s+")[0] }

if (-not $devices) {
    Write-Error "No devices connected. Please connect your Meta Quest headset(s) via USB."
    exit 1
}

Write-Host "Found $($devices.Count) device(s)" -ForegroundColor Cyan

# Create output directory
$SessionDir = Join-Path $OutputDir $SessionName
if (-not (Test-Path $SessionDir)) {
    New-Item -ItemType Directory -Path $SessionDir -Force | Out-Null
}

Write-Host "Output directory: $SessionDir" -ForegroundColor Cyan

$DeviceIndex = 1
$TotalFiles = 0

foreach ($device in $devices) {
    $HeadsetDir = Join-Path $SessionDir "H$DeviceIndex"
    
    if (-not (Test-Path $HeadsetDir)) {
        New-Item -ItemType Directory -Path $HeadsetDir -Force | Out-Null
    }
    
    Write-Host "`nProcessing device $DeviceIndex ($device)..." -ForegroundColor Yellow
    
    # Get device info
    $DeviceModel = & $Adb -s $device shell getprop ro.product.model 2>$null
    $AndroidVersion = & $Adb -s $device shell getprop ro.build.version.release 2>$null
    $BatteryLevel = & $Adb -s $device shell "dumpsys battery | grep level" 2>$null
    
    $DeviceInfo = @{
        serial = $device
        model = $DeviceModel.Trim()
        androidVersion = $AndroidVersion.Trim()
        batteryLevel = $BatteryLevel.Trim()
        extractedAt = (Get-Date -Format "o")
    }
    
    $DeviceInfoPath = Join-Path $HeadsetDir "device_info.json"
    $DeviceInfo | ConvertTo-Json | Set-Content $DeviceInfoPath
    Write-Host "  Saved device info" -ForegroundColor Gray
    
    # Pull metrics files
    $RemoteMetricsDir = "/sdcard/Android/data/$PackageName/files/metrics/"
    
    # List files in metrics directory
    $RemoteFiles = & $Adb -s $device shell "ls $RemoteMetricsDir 2>/dev/null" 2>$null
    
    if ($RemoteFiles) {
        Write-Host "  Found metrics files:" -ForegroundColor Green
        
        foreach ($file in ($RemoteFiles -split "`n" | Where-Object { $_.Trim() })) {
            $file = $file.Trim()
            if ($file) {
                $RemotePath = "$RemoteMetricsDir$file"
                $LocalPath = Join-Path $HeadsetDir $file
                
                Write-Host "    Pulling: $file" -ForegroundColor Gray
                & $Adb -s $device pull $RemotePath $LocalPath 2>$null
                
                if (Test-Path $LocalPath) {
                    $TotalFiles++
                }
            }
        }
    } else {
        Write-Host "  No metrics files found" -ForegroundColor Yellow
    }
    
    $DeviceIndex++
}

# Create session summary
$SessionInfo = @{
    sessionName = $SessionName
    extractedAt = (Get-Date -Format "o")
    deviceCount = $devices.Count
    totalFilesExtracted = $TotalFiles
}

$SessionInfoPath = Join-Path $SessionDir "session_info.json"
$SessionInfo | ConvertTo-Json | Set-Content $SessionInfoPath

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Extraction complete!" -ForegroundColor Green
Write-Host "  Session: $SessionName" -ForegroundColor White
Write-Host "  Devices: $($devices.Count)" -ForegroundColor White
Write-Host "  Files extracted: $TotalFiles" -ForegroundColor White
Write-Host "  Output: $SessionDir" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan

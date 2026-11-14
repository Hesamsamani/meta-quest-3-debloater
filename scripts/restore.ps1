<#
.SYNOPSIS
  Restores (re-enables) all disabled Meta Quest packages from catalog or backup.
#>
param([string]$Serial = '')
$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$catalog = Get-Content (Join-Path $root 'configs\packages-catalog.json') -Encoding UTF8 | ConvertFrom-Json

$adb = (Get-Command adb -ErrorAction SilentlyContinue).Source
if (-not $adb) {
    $candidates = @(
        (Join-Path $env:LOCALAPPDATA 'Programs\VRmoo\adb\adb.exe'),
        (Join-Path $env:LOCALAPPDATA 'Android\Sdk\platform-tools\adb.exe'),
        (Join-Path $env:USERPROFILE 'platform-tools\adb.exe')
    )
    $adb = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $adb) { throw 'adb.exe not found.' }

$devLines = & $adb devices | Where-Object { $_ -match '\tdevice$' }
if (-not $Serial) {
    if (-not $devLines) { throw 'No authorized Quest device connected via ADB.' }
    $Serial = ($devLines[0] -split '\s+')[0]
}

$disabledNow = @(& $adb -s $Serial shell pm list packages -d | ForEach-Object { ($_ -replace '^package:', '').Trim() })
$targets = $catalog.packages | Where-Object { $_.tier -ne 'PROTECTED' -and ($disabledNow -contains $_.package) }

Write-Host "Re-enabling $($targets.Count) catalog packages on $Serial..." -ForegroundColor Cyan
foreach ($p in $targets) {
    & $adb -s $Serial shell "pm enable $($p.package)" | Out-Null
    Write-Host "  [ENABLED] $($p.package)" -ForegroundColor Green
}
Write-Host 'All catalog packages restored.' -ForegroundColor Green

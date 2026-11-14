<#
.SYNOPSIS
  Reversible Meta Quest 3 / 3S / 2 debloater script using ADB `pm disable-user --user 0`.
.EXAMPLE
  pwsh .\scripts\debloat.ps1 -Preset safe
  pwsh .\scripts\debloat.ps1 -Preset pcvr -IncludeCaution
#>
param(
    [ValidateSet('telemetry', 'safe', 'pcvr')]
    [string]$Preset = 'safe',
    [string]$Serial = '',
    [switch]$DryRun
)
$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$catalogPath = Join-Path $root 'configs\packages-catalog.json'
if (-not (Test-Path $catalogPath)) { throw "Missing $catalogPath" }
$catalog = Get-Content $catalogPath -Encoding UTF8 | ConvertFrom-Json

$adb = (Get-Command adb -ErrorAction SilentlyContinue).Source
if (-not $adb) {
    $candidates = @(
        (Join-Path $env:LOCALAPPDATA 'Programs\VRmoo\adb\adb.exe'),
        (Join-Path $env:LOCALAPPDATA 'Android\Sdk\platform-tools\adb.exe'),
        (Join-Path $env:USERPROFILE 'platform-tools\adb.exe'),
        'C:\platform-tools\adb.exe'
    )
    $adb = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $adb) { throw 'adb.exe not found in PATH or standard locations.' }

$devLines = & $adb devices | Where-Object { $_ -match '\tdevice$' }
if (-not $Serial) {
    if (-not $devLines) { throw 'No authorized Quest device connected via ADB.' }
    $Serial = ($devLines[0] -split '\s+')[0]
}

Write-Host "Using ADB: $adb  |  Device: $Serial  |  Preset: $Preset" -ForegroundColor Cyan

$installed = @(& $adb -s $Serial shell pm list packages | ForEach-Object { ($_ -replace '^package:', '').Trim() })
$backupDir = Join-Path $root 'backups'
New-Item -ItemType Directory -Force $backupDir | Out-Null
$beforeTxt = Join-Path $backupDir "quest3-$Serial-before.txt"
if (-not (Test-Path $beforeTxt)) {
    & $adb -s $Serial shell pm list packages -d | Set-Content $beforeTxt -Encoding UTF8
    Write-Host "Saved pre-debloat backup -> $beforeTxt" -ForegroundColor Green
}

$targets = $catalog.packages | Where-Object {
    $_.tier -ne 'PROTECTED' -and ($_.presets -contains $Preset) -and ($installed -contains $_.package)
}

Write-Host "Disabling $($targets.Count) installed packages..." -ForegroundColor Yellow
foreach ($p in $targets) {
    $pkg = $p.package
    if ($DryRun) {
        Write-Host "  [DRY-RUN] pm disable-user --user 0 $pkg  ($($p.name))"
        continue
    }
    & $adb -s $Serial shell "pm disable-user --user 0 $pkg && am force-stop $pkg" | Out-Null
    $homeAct = (& $adb -s $Serial shell "cmd package resolve-activity --brief -c android.intent.category.HOME -a android.intent.action.MAIN" | Select-Object -Last 1).Trim()
    if ($homeAct -notlike 'com.oculus.vrshell/*') {
        Write-Warning "HOME changed to '$homeAct' after $pkg! Rolling back immediately..."
        & $adb -s $Serial shell "pm enable $pkg" | Out-Null
        throw "Stopped on safety rollback for $pkg"
    }
    Write-Host "  [DISABLED] $pkg  - $($p.name)" -ForegroundColor Green
}

Write-Host "`nDone! Undo anytime with: pwsh .\scripts\restore.ps1" -ForegroundColor Cyan

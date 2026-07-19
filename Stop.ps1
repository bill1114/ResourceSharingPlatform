# Stop.ps1 - Stop the running ResourceSharingPlatform instance
# Run via Stop.bat (which bypasses PowerShell execution policy for this session only)

$ErrorActionPreference = 'Continue'

# $RootDir is set by Stop.bat before this script's content is piped in via
# Invoke-Expression (this file is never executed directly as a .ps1 file).
if (-not $RootDir) { $RootDir = Split-Path -Parent $PSCommandPath }
$PidFile = Join-Path $RootDir '.run.pid'

Write-Host '=== ResourceSharingPlatform - Stop ===' -ForegroundColor Cyan

$stoppedAny = $false

if (Test-Path $PidFile) {
    $trackedPid = Get-Content $PidFile -ErrorAction SilentlyContinue
    if ($trackedPid) {
        $proc = Get-Process -Id $trackedPid -ErrorAction SilentlyContinue
        if ($proc) {
            Write-Host "Stopping tracked process (PID $trackedPid)..." -ForegroundColor Cyan
            Stop-Process -Id $trackedPid -Force -ErrorAction SilentlyContinue
            $stoppedAny = $true
        } else {
            Write-Host "Tracked PID $trackedPid is not running." -ForegroundColor Yellow
        }
    }
    Remove-Item $PidFile -Force -ErrorAction SilentlyContinue
}

# Fallback: catch any stray instance not tracked by the PID file
$stray = Get-Process -Name 'ResourceSharingPlatform' -ErrorAction SilentlyContinue
if ($stray) {
    foreach ($p in $stray) {
        Write-Host "Stopping stray process (PID $($p.Id))..." -ForegroundColor Cyan
        Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
        $stoppedAny = $true
    }
}

if ($stoppedAny) {
    Write-Host "`nApplication stopped." -ForegroundColor Green
} else {
    Write-Host "`nNo running instance was found." -ForegroundColor Yellow
}

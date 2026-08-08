# RegisterBackupTask.ps1 - One-time setup: registers a Windows Task Scheduler job that runs
# Backup.ps1 every Sunday at 02:00, as NT AUTHORITY\SYSTEM.
#
# Run once via RegisterBackupTask.bat, from an elevated (Administrator) Command Prompt
# (the .bat pipes this file's content into PowerShell instead of running it with -File, so
# it works even when a machine/user policy such as AllSigned blocks unsigned .ps1 files -
# same reasoning as Start.bat/Stop.bat, see the comment there).
#
# Why SYSTEM: it lets the task run unattended on a machine that is not always logged in,
# without ever storing a password. SYSTEM already has (or is granted, see BackupPlan.md)
# a LocalSupplyDB login in the db_backupoperator role and NTFS write access to the Backups
# folder, so it needs no further setup beyond what this script and the SQL grant already do.
#
# Re-running this script is safe - it replaces the existing task definition if one already
# exists (Register-ScheduledTask -Force).

$ErrorActionPreference = 'Stop'

# $RootDir is set by RegisterBackupTask.bat before this script's content is piped in via
# Invoke-Expression (this file is never executed directly as a .ps1 file).
if (-not $RootDir) { $RootDir = Split-Path -Parent $PSCommandPath }
$TaskName = 'ResourceSharingPlatform-Backup'
$ScriptPath = Join-Path $RootDir 'Backup.ps1'

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host 'ERROR: This script must be run from an elevated (Administrator) PowerShell prompt.' -ForegroundColor Red
    exit 1
}

$actionArgs = "-NoProfile -ExecutionPolicy Bypass -File ""$ScriptPath"""
$action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument $actionArgs
$trigger = New-ScheduledTaskTrigger -Weekly -DaysOfWeek Sunday -At 02:00
$principal = New-ScheduledTaskPrincipal -UserId 'NT AUTHORITY\SYSTEM' -LogonType ServiceAccount -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -DontStopOnIdleEnd -ExecutionTimeLimit (New-TimeSpan -Hours 1)
$description = '每週備份 LocalSupplyDB 資料庫與 Pictures 圖片資料夾，保留最近 14 份。'

$registerArgs = @{
    TaskName    = $TaskName
    Action      = $action
    Trigger     = $trigger
    Principal   = $principal
    Settings    = $settings
    Description = $description
    Force       = $true
}
Register-ScheduledTask @registerArgs | Out-Null

Write-Host "Scheduled task registered: runs Backup.ps1 every Sunday at 02:00 as SYSTEM." -ForegroundColor Green
Write-Host "To trigger it immediately for testing: Start-ScheduledTask -TaskName $TaskName" -ForegroundColor Cyan
Write-Host "To check its last run result: Get-ScheduledTaskInfo -TaskName $TaskName" -ForegroundColor Cyan

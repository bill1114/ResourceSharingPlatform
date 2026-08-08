@echo off
REM RegisterBackupTask.bat - One-time setup for the weekly backup schedule.
REM Must be run from an elevated (Administrator) Command Prompt: right-click this file
REM and choose "Run as administrator", or run it from an already-elevated prompt.
REM
REM The script content is piped into PowerShell instead of run with -File, so it works
REM even when a machine/user policy (e.g. AllSigned via Group Policy) blocks execution of
REM unsigned .ps1 files - same reasoning as Start.bat/Stop.bat. -Encoding UTF8 is required
REM on the Get-Content call because this script's Chinese task description would otherwise
REM be misread using the console's legacy codepage and corrupt the pasted script text.

setlocal
set SCRIPT_DIR=%~dp0

powershell -NoProfile -Command "$RootDir = '%SCRIPT_DIR%'; Get-Content -Raw -Encoding UTF8 '%SCRIPT_DIR%RegisterBackupTask.ps1' | Invoke-Expression"

echo.
pause

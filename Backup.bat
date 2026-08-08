@echo off
REM Backup.bat - Manually trigger an immediate backup (same script the weekly schedule runs).
REM The script content is piped into PowerShell instead of run with -File, so it works even
REM when a machine/user policy (e.g. AllSigned via Group Policy) blocks execution of unsigned
REM .ps1 files - same reasoning as Start.bat/Stop.bat.

setlocal
set SCRIPT_DIR=%~dp0

powershell -NoProfile -Command "Get-Content -Raw -Encoding UTF8 '%SCRIPT_DIR%Backup.ps1' | Invoke-Expression"

echo.
pause

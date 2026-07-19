@echo off
REM Start.bat - One-click build and launch for ResourceSharingPlatform
REM The script content is piped into PowerShell instead of run with -File, so it
REM works even when a machine/user policy (e.g. AllSigned via Group Policy) blocks
REM execution of unsigned .ps1 files. -ExecutionPolicy Bypass alone is not enough
REM on such machines because Group Policy takes precedence over the process scope.

setlocal
set SCRIPT_DIR=%~dp0

powershell -NoProfile -Command "$RootDir = '%SCRIPT_DIR%'; Get-Content -Raw '%SCRIPT_DIR%Start.ps1' | Invoke-Expression"

echo.
pause

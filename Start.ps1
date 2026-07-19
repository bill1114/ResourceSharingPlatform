# Start.ps1 - Build and launch ResourceSharingPlatform
# Run via Start.bat (which bypasses PowerShell execution policy for this session only)

$ErrorActionPreference = 'Stop'

# $RootDir is set by Start.bat before this script's content is piped in via
# Invoke-Expression (this file is never executed directly as a .ps1 file).
if (-not $RootDir) { $RootDir = Split-Path -Parent $PSCommandPath }
$ProjectDir  = Join-Path $RootDir 'ResourceSharingPlatform'
$CsprojPath  = Join-Path $ProjectDir 'ResourceSharingPlatform.csproj'
$ExePath     = Join-Path $ProjectDir 'bin\Debug\net8.0\ResourceSharingPlatform.exe'
$PidFile     = Join-Path $RootDir '.run.pid'
$AppUrl      = 'http://localhost:5140'

Write-Host '=== ResourceSharingPlatform - Start ===' -ForegroundColor Cyan

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host 'ERROR: .NET SDK (dotnet) was not found on PATH. Please install the .NET 8 SDK first.' -ForegroundColor Red
    exit 1
}

# If a previous run is still alive, don't start a second instance
if (Test-Path $PidFile) {
    $existingPid = Get-Content $PidFile -ErrorAction SilentlyContinue
    if ($existingPid -and (Get-Process -Id $existingPid -ErrorAction SilentlyContinue)) {
        Write-Host "The application is already running (PID $existingPid)." -ForegroundColor Yellow
        Start-Process $AppUrl
        exit 0
    }
    Remove-Item $PidFile -Force -ErrorAction SilentlyContinue
}

# Restore + build
Write-Host "`n[1/3] Restoring NuGet packages..." -ForegroundColor Cyan
dotnet restore $CsprojPath
if ($LASTEXITCODE -ne 0) { Write-Host 'ERROR: dotnet restore failed.' -ForegroundColor Red; exit 1 }

Write-Host "`n[2/3] Building project (Debug)..." -ForegroundColor Cyan
dotnet build $CsprojPath -c Debug --no-restore
if ($LASTEXITCODE -ne 0) { Write-Host 'ERROR: dotnet build failed.' -ForegroundColor Red; exit 1 }

# Best-effort database setup - never blocks app startup
Write-Host "`n[Optional] Checking local SQL Server database..." -ForegroundColor Cyan
$sqlScript = Join-Path $ProjectDir 'Database\CreateDatabase.sql'
if (Get-Command sqlcmd -ErrorAction SilentlyContinue) {
    try {
        sqlcmd -S . -E -i $sqlScript -b
        if ($LASTEXITCODE -eq 0) {
            Write-Host 'Database check/creation finished.' -ForegroundColor Green
        } else {
            Write-Host 'WARNING: sqlcmd reported an error while creating the database. The app will still start; fix the database manually if needed.' -ForegroundColor Yellow
        }
    } catch {
        Write-Host "WARNING: Could not run database setup script ($($_.Exception.Message)). The app will still start." -ForegroundColor Yellow
    }
} else {
    Write-Host 'sqlcmd not found on PATH - skipping automatic database setup. Run Database\CreateDatabase.sql manually if needed.' -ForegroundColor Yellow
}

# Launch the built app as its own process so Stop.bat can track/stop it cleanly
Write-Host "`n[3/3] Starting application..." -ForegroundColor Cyan
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = $AppUrl

$proc = Start-Process -FilePath $ExePath -WorkingDirectory $ProjectDir -WindowStyle Normal -PassThru
Set-Content -Path $PidFile -Value $proc.Id

Write-Host "Application started (PID $($proc.Id))." -ForegroundColor Green

# Wait for the server to respond, then open the browser
$ready = $false
for ($i = 0; $i -lt 20; $i++) {
    Start-Sleep -Milliseconds 500
    try {
        $response = Invoke-WebRequest -Uri $AppUrl -UseBasicParsing -TimeoutSec 2
        if ($response.StatusCode -ge 200) { $ready = $true; break }
    } catch {
        # not ready yet, keep polling
    }
}

if ($ready) {
    Write-Host "Server is ready at $AppUrl" -ForegroundColor Green
} else {
    Write-Host "Server did not respond within the timeout, but the process is running. Check the app window for errors." -ForegroundColor Yellow
}

Start-Process $AppUrl
Write-Host "`nDone. Use Stop.bat to stop the application." -ForegroundColor Cyan

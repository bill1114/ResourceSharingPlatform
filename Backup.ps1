# Backup.ps1 - Weekly backup of the LocalSupplyDB database and the Pictures upload folder
# Run manually via: powershell -ExecutionPolicy Bypass -File Backup.ps1
# Run automatically by the "ResourceSharingPlatform-Backup" scheduled task (see RegisterBackupTask.ps1),
# which executes this script as NT AUTHORITY\SYSTEM every Sunday at 02:00.
#
# Produces, per run:
#   Backups\LocalSupplyDB_yyyyMMdd_HHmmss.bak
#   Backups\Pictures_yyyyMMdd_HHmmss.zip
# and keeps only the newest 14 of each (older ones are deleted). Both steps are independent -
# a failure in one does not stop the other from being attempted. Every run appends a line to
# Backups\backup.log regardless of outcome.

$ErrorActionPreference = 'Continue'

$BackupDir   = 'D:\ResourceSharingPlatform\Backups'
$PicturesDir = 'D:\ResourceSharingPlatform\Pictures'
$DbName      = 'LocalSupplyDB'
$SqlServer   = '.'
$RetainCount = 14
$LogFile     = Join-Path $BackupDir 'backup.log'
$Timestamp   = Get-Date -Format 'yyyyMMdd_HHmmss'

if (-not (Test-Path $BackupDir)) {
    New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
}

function Write-Log {
    param([string]$Message)
    $line = "[{0}] {1}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $Message
    # Add-Content can intermittently throw "Stream was not readable" when called in quick
    # succession in some hosting contexts (e.g. via Invoke-Expression, or under the Task
    # Scheduler service host) - use a plain .NET file append instead, which is more reliable
    # for this unattended, no-one-watching-the-console scenario.
    [System.IO.File]::AppendAllText($LogFile, $line + [Environment]::NewLine)
    Write-Host $line
}

Write-Log "=== Backup run started ==="

# --- Step 1: Database backup ---
try {
    $bakPath = Join-Path $BackupDir "${DbName}_$Timestamp.bak"
    $query = "BACKUP DATABASE [$DbName] TO DISK = N'$bakPath' WITH COPY_ONLY, INIT, NAME = N'$DbName-Full-$Timestamp'"

    if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
        throw "sqlcmd was not found on PATH."
    }

    sqlcmd -S $SqlServer -E -Q $query -b
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd exited with code $LASTEXITCODE."
    }

    $bakFile = Get-Item $bakPath -ErrorAction Stop
    if ($bakFile.Length -le 0) {
        throw "Backup file was created but is 0 bytes."
    }

    Write-Log "Database backup OK: $bakPath ($([math]::Round($bakFile.Length / 1MB, 2)) MB)"
} catch {
    Write-Log "ERROR: Database backup FAILED - $($_.Exception.Message)"
}

# --- Step 2: Pictures folder backup ---
try {
    if (-not (Test-Path $PicturesDir)) {
        throw "Pictures folder not found at $PicturesDir."
    }

    $zipPath = Join-Path $BackupDir "Pictures_$Timestamp.zip"
    Compress-Archive -Path (Join-Path $PicturesDir '*') -DestinationPath $zipPath -CompressionLevel Optimal -ErrorAction Stop

    $zipFile = Get-Item $zipPath -ErrorAction Stop
    if ($zipFile.Length -le 0) {
        throw "Zip file was created but is 0 bytes."
    }

    Write-Log "Pictures backup OK: $zipPath ($([math]::Round($zipFile.Length / 1MB, 2)) MB)"
} catch {
    Write-Log "ERROR: Pictures backup FAILED - $($_.Exception.Message)"
}

# --- Step 3: Retention cleanup (keep newest N of each type) ---
function Remove-OldBackups {
    param([string]$Filter)
    $files = Get-ChildItem -Path $BackupDir -Filter $Filter -File | Sort-Object LastWriteTime -Descending
    if ($files.Count -le $RetainCount) { return }
    $toDelete = $files | Select-Object -Skip $RetainCount
    foreach ($f in $toDelete) {
        try {
            Remove-Item $f.FullName -Force -ErrorAction Stop
            Write-Log "Retention cleanup: deleted $($f.Name)"
        } catch {
            Write-Log "ERROR: Retention cleanup could not delete $($f.Name) - $($_.Exception.Message)"
        }
    }
}

try {
    Remove-OldBackups -Filter "${DbName}_*.bak"
    Remove-OldBackups -Filter 'Pictures_*.zip'
} catch {
    Write-Log "ERROR: Retention cleanup FAILED - $($_.Exception.Message)"
}

Write-Log "=== Backup run finished ==="

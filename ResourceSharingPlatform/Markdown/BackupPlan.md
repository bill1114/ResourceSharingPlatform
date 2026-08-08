# 資料庫與圖片備份筆記

本機（LAPTOP-FMFDKESG）的每週自動備份設定：資料庫 `LocalSupplyDB` 與圖片資料夾 `Pictures`。

## 目前設定

| 項目 | 值 |
|---|---|
| 備份腳本 | `Backup.ps1`（repo 根目錄，已納入版控） |
| 排程工作 | Windows工作排程器「`ResourceSharingPlatform-Backup`」 |
| 排程時間 | 每週日凌晨 02:00 |
| 執行身分 | `NT AUTHORITY\SYSTEM`（不需存密碼，機器沒登入也能跑） |
| 備份內容 | `LocalSupplyDB` 全量備份（`.bak`）＋ `Pictures\` 整包壓縮（`.zip`） |
| 存放位置 | `D:\ResourceSharingPlatform\Backups\` |
| 保留份數 | 最近 14 份（`.bak` 與 `.zip` 分開各留 14 份，超過自動刪除最舊的） |
| 執行紀錄 | `Backups\backup.log`（每次執行都會附加，不會覆蓋） |

備份檔名格式：`LocalSupplyDB_yyyyMMdd_HHmmss.bak`、`Pictures_yyyyMMdd_HHmmss.zip`。

## 權限設定（一次性，已完成）

- SQL Server：`NT AUTHORITY\SYSTEM` 登入已加入 `LocalSupplyDB` 的 `db_backupoperator` 角色（只給備份權限，不給讀寫資料的權限），比照 IIS App Pool 登入的最小權限做法（見 `IISDeployment.md`）。
- 檔案系統：`icacls` 已授權 SQL Server 服務帳號（`NT Service\MSSQL$SQLEXPRESS`）與 `SYSTEM` 對 `D:\ResourceSharingPlatform\Backups\` 的寫入權限。

這兩項是一次性設定，正常情況不需要重做；如果之後在別台機器上重新部署，才需要重新執行（見下方「在新機器上設定」）。

## 手動立即執行一次備份

雙擊 `Backup.bat`（或用系統管理員權限的命令提示字元執行），會立刻備份一次並印出結果；也可以直接用 PowerShell：

```powershell
powershell -ExecutionPolicy Bypass -File D:\ResourceSharingPlatform\ResourceSharingPlatform\Backup.ps1
```

## 確認排程是否正常執行

```powershell
# 查看上次執行時間與結果（0 表示成功）
Get-ScheduledTaskInfo -TaskName "ResourceSharingPlatform-Backup"

# 立即手動觸發一次排程（用來測試 SYSTEM 身分執行是否正常，不用等到週日）
Start-ScheduledTask -TaskName "ResourceSharingPlatform-Backup"
```

或直接看 `Backups\backup.log` 最後幾行、`Backups\` 資料夾內最新的 `.bak`／`.zip` 時間戳記。

## 還原方式

### 還原資料庫

```powershell
# 1. 先停用 App Pool／關閉 Kestrel，避免還原時有連線占用資料庫
Stop-WebAppPool -Name ResourceSharingPlatformPool
# 開發用 Kestrel 用 Stop.bat 關閉

# 2. 還原（WITH REPLACE 會覆蓋現有資料庫，還原前務必確認檔名與時間點正確）
sqlcmd -S . -E -Q "RESTORE DATABASE LocalSupplyDB FROM DISK = 'D:\ResourceSharingPlatform\Backups\LocalSupplyDB_20260809_020000.bak' WITH REPLACE"

# 3. 重啟服務
Start-WebAppPool -Name ResourceSharingPlatformPool
```

### 還原圖片

把對應時間點的 `Pictures_yyyyMMdd_HHmmss.zip` 解壓縮，內容直接覆蓋回 `D:\ResourceSharingPlatform\Pictures\`（先確認要不要保留還原時間點之後新增的圖片，必要時先備份現有資料夾再覆蓋）。

## 在新機器上設定（一次性）

1. 確認 `appsettings.json` 的 `UploadsRoot` 與本文件的路徑一致，`Backups\` 資料夾存在。
2. 建立 SQL 登入並加入 `db_backupoperator`：
   ```sql
   CREATE LOGIN [NT AUTHORITY\SYSTEM] FROM WINDOWS; -- 通常已內建存在，可省略
   USE LocalSupplyDB;
   CREATE USER [NT AUTHORITY\SYSTEM] FOR LOGIN [NT AUTHORITY\SYSTEM];
   ALTER ROLE db_backupoperator ADD MEMBER [NT AUTHORITY\SYSTEM];
   ```
3. 授權備份資料夾：
   ```powershell
   icacls "D:\ResourceSharingPlatform\Backups" /grant "NT Service\MSSQL`$SQLEXPRESS:(OI)(CI)M" /grant "SYSTEM:(OI)(CI)F"
   ```
4. 用系統管理員權限執行 `RegisterBackupTask.bat` 註冊排程（可重複執行，會覆蓋舊的排程定義）。

## 注意事項

- 備份用 `COPY_ONLY`，不會影響正式的差異備份鏈（目前沒有另外的差異備份機制，純粹保留彈性）。
- 資料庫備份與圖片備份各自獨立 try/catch，其中一個失敗不影響另一個；任何失敗都會寫進 `backup.log`，需要定期人工檢查（目前沒有失敗通知機制）。
- `Backups\` 資料夾本身不在版控範圍內，也不在 `dotnet publish` 的輸出範圍內。

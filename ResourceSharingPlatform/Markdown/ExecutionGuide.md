# 地方物資管理平台－執行、打包與部署指南

最後更新：2026-08-09

## 環境需求

- Windows 10/11 或 Windows Server
- .NET 8 SDK（建置）或 ASP.NET Core 8 Hosting Bundle（IIS 執行）
- SQL Server
- `sqlcmd`（使用自動建庫腳本時）
- Visual Studio 2022 可選

## 開發環境啟動

在方案根目錄執行：

```powershell
.\Start.bat
```

腳本會：

1. 還原 NuGet 套件。
2. 建置 Debug 版本。
3. 執行 `Database/CreateDatabase.sql`。
4. 啟動 `http://localhost:5140`。
5. 將 PID 寫入 `.run.pid`。

停止：

```powershell
.\Stop.bat
```

## 手動建置

```powershell
dotnet restore .\ResourceSharingPlatform\ResourceSharingPlatform.csproj
dotnet build .\ResourceSharingPlatform\ResourceSharingPlatform.csproj -c Release --no-restore
```

## 建立全新資料庫

```powershell
sqlcmd -S . -E -i .\ResourceSharingPlatform\Database\CreateDatabase.sql -b
```

預設資料庫名稱為 `LocalSupplyDB`。若 SQL Server 不在本機，請調整 `appsettings.json` 的 `DefaultConnection`，並以對應伺服器執行 SQL。

## 舊版資料庫升級

應用程式啟動時會以 idempotent SQL：

- 補建物資定義、規格、據點安全庫存資料表。
- 補上 `SupplyItem.InventoryItemVariantId`。
- 清除已於 2026-08 移除的過渡表 `InventoryTypeSetting`（若既有安裝仍殘留）。

正式環境升級前仍必須先完整備份資料庫；每週自動備份設定見 `Markdown/BackupPlan.md`。

## 發布打包

```powershell
dotnet publish .\ResourceSharingPlatform\ResourceSharingPlatform.csproj `
  -c Release `
  -o .\publish `
  --no-restore
```

發布包至少包含：

- 應用程式 DLL、runtimeconfig 與 deps 檔
- `Views` 編譯產物
- `wwwroot`
- `appsettings.json`
- `Database/CreateDatabase.sql`
- 本操作文件

## 打包前資料備份

```text
LocalSupplyDB
ResourceSharingPlatform/wwwroot/uploads/items
ResourceSharingPlatform/wwwroot/uploads/ai-stockin
```

圖片不在 SQL Server 內，不能只備份資料庫。

## 初次使用順序

1. 使用管理員登入。
2. 建立據點。
3. 到「系統管理 → 庫存種類設定」建立物資定義。
4. 設定規格。
5. 設定各據點安全庫存。
6. 到「物資管理 → 新增物資」選擇標準物資與規格。
7. 檢查 Dashboard 的據點低庫存與全系統總量不足。

## 驗收清單

- [ ] 登入與角色權限正確
- [ ] 據點與地圖正常
- [ ] 物資定義、規格與據點門檻可維護
- [ ] 新增物資只能選擇啟用中的標準規格
- [ ] 相同物資不同規格會合計判斷安全庫存
- [ ] 全系統目前數量不含調撥中數量
- [ ] 調撥、出庫、捐贈與報廢正確增減庫存
- [ ] 即期與過期判斷正確
- [ ] Excel 可下載
- [ ] 圖片在重新部署後仍存在

## 正式環境安全事項

- 不可保留預設管理員密碼。
- 連線字串與 API Key 應使用環境變數或 Secret Store。
- `ChannelAccessToken`、`ChannelSecret`、`ApiKey` 不應以正式明文值放入發布包。
- 正式站應使用 HTTPS。
- uploads 目錄需限制可執行檔案並定期掃描與備份。

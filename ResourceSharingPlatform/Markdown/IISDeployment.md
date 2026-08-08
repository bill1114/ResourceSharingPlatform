# IIS 部署筆記

本機（LAPTOP-FMFDKESG）上的正式站台，與 `Start.bat`／`Stop.bat` 的開發用 Kestrel（port 5140）並存，互不影響。

## 目前設定

| 項目 | 值 |
|---|---|
| IIS 站台 | `ResourceSharingPlatform` |
| Application Pool | `ResourceSharingPlatformPool`（No Managed Code，Integrated 管線） |
| 實體路徑 | `D:\ResourceSharingPlatform0807\publish` |
| 綁定 | `http://localhost:8081`（僅限本機，未對外/對區網開放） |
| 資料庫存取 | SQL Server 登入 `IIS APPPOOL\ResourceSharingPlatformPool`（Windows 驗證），已加入 `LocalSupplyDB` 的 `db_datareader`／`db_datawriter` |

## 更新部署（每次要把最新程式碼發布到 IIS 時）

```powershell
# 1. 發布最新版本到既有的 publish 資料夾
cd D:\ResourceSharingPlatform0807\ResourceSharingPlatform\ResourceSharingPlatform
dotnet publish -c Release -o D:\ResourceSharingPlatform0807\publish

# 2. 重啟 App Pool 讓新版生效
Import-Module WebAdministration
Restart-WebAppPool -Name ResourceSharingPlatformPool
```

`dotnet publish` 只會覆蓋程式檔案，不會動到圖片（圖片現在存在 wwwroot 之外，見下）。

## 圖片統一存放位置

原本開發用（Kestrel）跟 IIS 各有一份 `wwwroot\uploads`，兩邊上傳的圖片互相看不到、`dotnet publish` 也不會同步。現在改成兩邊都讀寫同一個外部資料夾：

- 實體路徑：`D:\ResourceSharingPlatform0807\Pictures\`（`items\` 一般物資圖片、`ai-stockin\` AI 智慧入庫照片）
- 由 `appsettings.json` 的 `"UploadsRoot"` 設定指定，`Services/UploadPathProvider.cs` 讀取；`Program.cs` 額外掛一個指到這個資料夾的 `/uploads` 靜態檔案中介軟體
- 如果 `UploadsRoot` 沒設定，會自動退回原本的 `wwwroot/uploads`（相容性保底）
- 這個資料夾在 `dotnet publish` 的輸出範圍之外，不會被覆蓋或清掉

## 注意事項

- 目前只綁定 `localhost:8081`，同機器以外（含同區網其他裝置）連不進來——這是刻意的，尚未申請對外/對區網開放。之後若要開放，需要：另外調整站台繫結（IP/Port 或改成 `*`）、開 Windows 防火牆對應規則。這兩件事都需要另外明確授權才會處理。
- `appsettings.json` 的連線字串沿用 `Trusted_Connection=True`，所以「誰能連資料庫」是靠上面那個 SQL 登入控管，不是帳密。
- ASPNETCORE_ENVIRONMENT 未特別設定，IIS 進程內預設為 `Production`（不會顯示詳細例外頁面）。

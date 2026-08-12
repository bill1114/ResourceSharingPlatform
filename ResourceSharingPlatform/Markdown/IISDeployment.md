# IIS 部署筆記

本機（目前主機名稱 `USER`）上的正式站台，與 `Start.bat`／`Stop.bat` 的開發用 Kestrel（port 5140）並存，互不影響。

> ⚠️ 2026-08-12 更新：實際盤點站台設定後，發現與本文件先前記載的內容（`localhost:8081`、App Pool `ResourceSharingPlatformPool`）不符，已依實際狀態全面更正。且站台現在**已對外網開放**，不再是僅限本機/區網——請詳閱下方「對外開放狀態」一節。

## 目前設定

| 項目 | 值 |
|---|---|
| IIS 站台 | `ResourceSharingPlatform`（Site ID 2） |
| Application Pool | `ResourceSharingPlatform`（注意：**不是** `ResourceSharingPlatformPool`，舊文件記錯了） |
| 實體路徑 | `D:\ResourceSharingPlatform\publish` |
| Hosting Model | `inprocess`（見 `web.config`） |
| 綁定 | `http://192.168.0.151:5000`（純 HTTP，未裝 SSL 憑證） |
| stdout 記錄 | 已開啟（`web.config` 的 `stdoutLogEnabled="true"`），log 存在 `publish\logs\stdout_*.log`，排查完問題後建議改回 `false`，避免 log 一直長 |
| 資料庫存取 | SQL Server 登入 `IIS APPPOOL\ResourceSharingPlatform`（Windows 驗證），已加入 `LocalSupplyDB` 的 **`db_owner`** 角色（因為程式啟動時會自動執行 migration/DDL，需要比讀寫更高的權限；若要收斂成最小權限，至少要 `db_datareader`＋`db_datawriter`＋`db_ddladmin`） |

## 對外開放狀態（2026-08-12 起）

這個站台現在**已經是可以從外部網路直接連線的公開服務**，不再是僅限本機/區網。相關設定：

| 項目 | 值 |
|---|---|
| 本機固定 IP | `192.168.0.151`／子網路遮罩 `255.255.255.0`／閘道 `192.168.0.1`／DNS `192.168.0.1` |
| Windows 防火牆 | 已新增輸入規則 `ResourceSharingPlatform-5000`（TCP 5000, Allow, 所有設定檔） |
| 路由器 Port Forwarding | 外部 TCP `5000` → `192.168.0.151:5000`（路由器型號：D-Link，區網主機名稱顯示為 `dlinkrouter`） |
| 目前公網 IP | `122.117.44.214`（**很可能是動態 IP**，之後若 IP 換了外部會連不到，需要另外設定 DDNS，多數家用路由器內建這個功能） |
| 外部存取網址 | `http://122.117.44.214:5000/`（**務必打 `http://`，不能用 `https://`**——目前沒有裝 SSL 憑證，用 https 連線會直接被拒絕） |

**已知待處理事項 / 風險：**

- ⚠️ 目前完全沒有 HTTPS，帳號密碼、Cookie 都是明文傳輸，正式對外服務前應該考慮申請憑證（例如用 Let's Encrypt / Certbot，或前面加一台有憑證的反向代理）。
- ⚠️ 曾發現手動設定固定 IP 時，`192.168.0.151` 一度被套用到「藍牙網路」介面而不是 Wi-Fi 介面（Windows 網路設定畫面容易選錯介面）。目前測試站台仍可正常回應，但建議之後找時間確認 IP 實際掛在 Wi-Fi 網卡上，避免藍牙功能被關掉時站台跟著斷線。
- 資料庫帳號目前是 `db_owner`（權限偏寬鬆），之後有空建議收斂成最小必要權限。
- 家用網路多半是動態 IP，沒設定 DDNS 的話，路由器重開機或 ISP 重新分配 IP 後，外部就連不到了。

## 更新部署（每次要把最新程式碼發布到 IIS 時）

```powershell
# 1. 發布最新版本到既有的 publish 資料夾
cd D:\ResourceSharingPlatform\ResourceSharingPlatform\ResourceSharingPlatform
dotnet publish -c Release -o D:\ResourceSharingPlatform\publish

# 2. 重啟 App Pool 讓新版生效
Import-Module WebAdministration
Restart-WebAppPool -Name ResourceSharingPlatform
```

`dotnet publish` 只會覆蓋程式檔案，不會動到圖片（圖片現在存在 wwwroot 之外，見下）。

> 注意：ASP.NET Core in-process 模式下，如果應用程式啟動失敗（例如資料庫連不上），該次的 worker process 會卡在失敗狀態，之後同一個 process 的請求都直接回 500.30、不會自動重試。修好問題（例如補上資料庫權限）之後，必須重新 `Restart-WebAppPool`（或讓 worker process 換掉）才會真的重新嘗試啟動。

## 常見故障排查（500.30 等啟動失敗）

1. 先看 `publish\logs\stdout_*.log`（若 `stdoutLogEnabled` 是 `true`）或 Windows 事件檢視器 →應用程式記錄，篩選 `IIS AspNetCore Module V2` / `.NET Runtime`，通常能直接看到完整例外訊息。
2. 常見原因是 SQL Server 登入權限：連線字串用 `Trusted_Connection=True`，實際連線身分是 `IIS APPPOOL\<App Pool 名稱>`，這個 Windows 帳號必須先在 SQL Server 建立登入並授權：
   ```sql
   CREATE LOGIN [IIS APPPOOL\ResourceSharingPlatform] FROM WINDOWS;
   USE LocalSupplyDB;
   CREATE USER [IIS APPPOOL\ResourceSharingPlatform] FOR LOGIN [IIS APPPOOL\ResourceSharingPlatform];
   ALTER ROLE db_owner ADD MEMBER [IIS APPPOOL\ResourceSharingPlatform];
   ```
3. 改完權限後別忘了 `Restart-WebAppPool -Name ResourceSharingPlatform`，理由見上一節的提醒。

## 圖片統一存放位置

原本開發用（Kestrel）跟 IIS 各有一份 `wwwroot\uploads`，兩邊上傳的圖片互相看不到、`dotnet publish` 也不會同步。現在改成兩邊都讀寫同一個外部資料夾：

- 實體路徑：`D:\ResourceSharingPlatform\Pictures\`（`items\` 一般物資圖片、`ai-stockin\` AI 智慧入庫照片）
- 由 `appsettings.json` 的 `"UploadsRoot"` 設定指定，`Services/UploadPathProvider.cs` 讀取；`Program.cs` 額外掛一個指到這個資料夾的 `/uploads` 靜態檔案中介軟體
- 如果 `UploadsRoot` 沒設定，會自動退回原本的 `wwwroot/uploads`（相容性保底）
- 這個資料夾在 `dotnet publish` 的輸出範圍之外，不會被覆蓋或清掉

## 注意事項

- `appsettings.json` 的連線字串沿用 `Trusted_Connection=True`，所以「誰能連資料庫」是靠上面那個 SQL 登入控管，不是帳密。
- ASPNETCORE_ENVIRONMENT 未特別設定，IIS 進程內預設為 `Production`（不會顯示詳細例外頁面）。

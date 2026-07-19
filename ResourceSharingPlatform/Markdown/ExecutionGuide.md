# 地方物資管理平台 - 執行步驟指南

## ?? 前置準備檢查清單

### 1. 開發環境確認
- [x] 已安裝 Visual Studio 2022
- [ ] 已安裝 .NET 8 SDK
- [ ] 已安裝 SQL Server（LocalDB、Express 或完整版皆可）
- [ ] 已安裝 SQL Server Management Studio (SSMS)

### 2. 確認版本
```powershell
# 檢查 .NET 版本
dotnet --version
# 應顯示 8.x.x

# 檢查 SQL Server
# 在 SSMS 中連線後執行：SELECT @@VERSION
```

---

## ?? 步驟一：還原 NuGet 套件

### 方法 1：使用 Visual Studio
1. 開啟 `ResourceSharingPlatform.sln`
2. 在 Solution Explorer 中右鍵點擊專案
3. 選擇「還原 NuGet 套件」

### 方法 2：使用命令列
```powershell
cd C:\Users\YJ\source\repos\ResourceSharingPlatform\ResourceSharingPlatform
dotnet restore
```

### 預期安裝的套件
- Microsoft.EntityFrameworkCore.SqlServer (8.0.0)
- Microsoft.EntityFrameworkCore.Tools (8.0.0)

---

## ??? 步驟二：建立資料庫

### 2.1 連線到 SQL Server
開啟 SQL Server Management Studio (SSMS)，連線到您的 SQL Server 實例：
- **伺服器名稱**：`.` 或 `(localdb)\MSSQLLocalDB` 或您的伺服器名稱
- **驗證**：Windows Authentication

### 2.2 執行資料庫建立腳本
1. 開啟檔案：`ResourceSharingPlatform\Database\CreateDatabase.sql`
2. 在 SSMS 中執行整個腳本（按 F5）
3. 確認訊息：「資料庫與資料表建立完成！」

### 2.3 驗證資料表
```sql
USE LocalSupplyDB;
GO

-- 檢查資料表是否建立
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

-- 應該顯示：
-- SupplyItem
-- SupplyLocation
-- SupplyTransferLog
-- UserAccount
```

### 2.4 載入測試資料
1. 開啟檔案：`ResourceSharingPlatform\Database\InsertTestData.sql`
2. 在 SSMS 中執行整個腳本（按 F5）
3. 確認訊息：「測試資料載入完成！」

### 2.5 驗證測試資料
```sql
USE LocalSupplyDB;
GO

-- 檢查據點數量（應為 3）
SELECT COUNT(*) FROM SupplyLocation;

-- 檢查物資數量（應為 5）
SELECT COUNT(*) FROM SupplyItem;

-- 查看完整資料
SELECT * FROM SupplyLocation;
SELECT * FROM SupplyItem;
```

---

## ?? 步驟三：設定連線字串

### 3.1 開啟 appsettings.json
檔案位置：`ResourceSharingPlatform\appsettings.json`

### 3.2 檢查連線字串
預設連線字串：
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=LocalSupplyDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 3.3 根據您的環境調整

#### 使用 SQL Server Express
```json
"Server=.\\SQLEXPRESS;Database=LocalSupplyDB;Trusted_Connection=True;TrustServerCertificate=True;"
```

#### 使用 LocalDB
```json
"Server=(localdb)\\MSSQLLocalDB;Database=LocalSupplyDB;Trusted_Connection=True;TrustServerCertificate=True;"
```

#### 使用具名 SQL Server 實例
```json
"Server=YOUR_SERVER_NAME;Database=LocalSupplyDB;Trusted_Connection=True;TrustServerCertificate=True;"
```

#### 使用 SQL 帳號密碼驗證
```json
"Server=.;Database=LocalSupplyDB;User Id=your_username;Password=your_password;TrustServerCertificate=True;"
```

---

## ??? 步驟四：建置專案

### 使用 Visual Studio
1. 開啟 `ResourceSharingPlatform.sln`
2. 按 `Ctrl + Shift + B` 建置方案
3. 檢查「輸出」視窗，確認建置成功

### 使用命令列
```powershell
cd C:\Users\YJ\source\repos\ResourceSharingPlatform\ResourceSharingPlatform
dotnet build
```

### 預期結果
```
建置成功。
    0 個警告
    0 個錯誤
```

---

## ?? 步驟五：執行專案

### 方法 1：使用 Visual Studio（推薦）
1. 按 `F5` 或點擊「開始偵錯」按鈕
2. 瀏覽器會自動開啟
3. 預設首頁：`https://localhost:xxxx/Dashboard`

### 方法 2：使用命令列
```powershell
cd C:\Users\YJ\source\repos\ResourceSharingPlatform\ResourceSharingPlatform
dotnet run
```

然後在瀏覽器開啟顯示的 URL（通常是 `https://localhost:5001` 或 `http://localhost:5000`）

---

## ? 步驟六：驗收測試

### 6.1 戰情總覽頁面
- [ ] 訪問首頁 `/Dashboard`
- [ ] 確認顯示 6 個統計卡片
- [ ] 確認顯示各據點統計表（3 個據點）
- [ ] 確認低庫存與即期物資列表

### 6.2 據點管理
- [ ] 訪問 `/SupplyLocation`
- [ ] 確認顯示 3 個據點
- [ ] 測試新增據點
- [ ] 測試編輯據點
- [ ] 測試查看詳細資料
- [ ] 測試刪除據點（軟刪除）

### 6.3 物資管理
- [ ] 訪問 `/SupplyItem`
- [ ] 確認顯示 5 筆物資
- [ ] 測試依據點篩選
- [ ] 測試依種類篩選
- [ ] 測試新增物資
- [ ] 測試編輯物資
- [ ] 確認狀態顯示正確（正常/低庫存/即期/過期）

### 6.4 物資轉移
- [ ] 訪問 `/SupplyTransfer/Create`
- [ ] 選擇物資、來源據點、目標據點
- [ ] 輸入轉移數量
- [ ] 確認轉移成功
- [ ] 檢查來源數量減少
- [ ] 檢查目標數量增加
- [ ] 在轉移紀錄中確認有新紀錄

### 6.5 轉移紀錄
- [ ] 訪問 `/SupplyTransfer`
- [ ] 確認顯示轉移紀錄
- [ ] 確認顯示完整資訊（時間、物資、來源、目標、數量、操作人員）

### 6.6 地圖總覽
- [ ] 訪問 `/Map`
- [ ] 確認地圖正常載入（OpenStreetMap）
- [ ] 確認顯示 3 個據點標記
- [ ] 點擊標記確認彈出視窗顯示據點資訊
- [ ] 點擊「查看物資明細」連結正常跳轉

---

## ?? 常見問題排除

### 問題 1：無法連線到資料庫
**錯誤訊息**：`A network-related or instance-specific error occurred`

**解決方法**：
1. 確認 SQL Server 服務正在執行
2. 檢查連線字串中的伺服器名稱
3. 確認 SQL Server 允許 TCP/IP 連線
4. 嘗試在 SSMS 中手動連線測試

### 問題 2：資料庫不存在
**錯誤訊息**：`Cannot open database "LocalSupplyDB"`

**解決方法**：
執行 `Database\CreateDatabase.sql` 建立資料庫

### 問題 3：資料表不存在
**錯誤訊息**：`Invalid object name 'SupplyLocation'`

**解決方法**：
確認已執行 `CreateDatabase.sql` 中的所有建表語句

### 問題 4：NuGet 套件未安裝
**錯誤訊息**：`The type or namespace name 'EntityFrameworkCore' could not be found`

**解決方法**：
```powershell
dotnet restore
```

### 問題 5：連接埠衝突
**錯誤訊息**：`Unable to bind to https://localhost:5001`

**解決方法**：
修改 `Properties\launchSettings.json` 中的連接埠號碼

### 問題 6：地圖無法顯示
**可能原因**：網路問題無法載入 Leaflet.js 或 OpenStreetMap

**解決方法**：
1. 檢查網路連線
2. 檢查瀏覽器主控台是否有 JavaScript 錯誤
3. 確認防火牆未封鎖外部 CDN

---

## ?? 資料庫連線測試

### 測試連線腳本
在專案根目錄建立 `TestConnection.sql`：

```sql
-- 測試連線
SELECT 
    'Connection Successful!' AS Message,
    @@VERSION AS SQLVersion,
    DB_NAME() AS CurrentDatabase;

-- 檢查資料表
SELECT 
    TABLE_NAME,
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = t.TABLE_NAME) AS ColumnCount
FROM INFORMATION_SCHEMA.TABLES t
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

-- 檢查資料筆數
SELECT 'SupplyLocation' AS TableName, COUNT(*) AS RecordCount FROM SupplyLocation
UNION ALL
SELECT 'SupplyItem', COUNT(*) FROM SupplyItem
UNION ALL
SELECT 'SupplyTransferLog', COUNT(*) FROM SupplyTransferLog
UNION ALL
SELECT 'UserAccount', COUNT(*) FROM UserAccount;
```

---

## ?? 開發模式設定

### 啟用詳細錯誤訊息
在 `Program.cs` 中，開發環境下會自動啟用詳細錯誤：

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
```

### 查看 EF Core 執行的 SQL
在 `appsettings.Development.json` 中加入：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

---

## ?? 瀏覽器測試建議

建議使用以下瀏覽器測試：
- Google Chrome（推薦）
- Microsoft Edge
- Firefox

---

## ?? 快速啟動檢查清單

執行前快速確認：
- [ ] SQL Server 服務正在執行
- [ ] 資料庫 `LocalSupplyDB` 已建立
- [ ] 測試資料已載入
- [ ] NuGet 套件已還原
- [ ] 專案可以成功建置
- [ ] 連線字串設定正確

全部打勾後即可執行 `F5` 啟動專案！

---

## ?? 取得協助

如果遇到問題：
1. 檢查「輸出」視窗的錯誤訊息
2. 檢查瀏覽器開發者工具的主控台
3. 檢查資料庫連線是否正常
4. 參考本文件的「常見問題排除」章節

---

## ?? 成功啟動後

訪問以下 URL 開始使用：
- 戰情總覽：`https://localhost:xxxx/Dashboard`
- 據點管理：`https://localhost:xxxx/SupplyLocation`
- 物資管理：`https://localhost:xxxx/SupplyItem`
- 物資轉移：`https://localhost:xxxx/SupplyTransfer/Create`
- 轉移紀錄：`https://localhost:xxxx/SupplyTransfer`
- 地圖總覽：`https://localhost:xxxx/Map`

享受使用！??

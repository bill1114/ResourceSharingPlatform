# 地方物資管理平台 - 開發進度追蹤

## 專案資訊
- **專案名稱**: ResourceSharingPlatform（地方物資管理平台）
- **技術堆疊**: ASP.NET Core MVC + .NET 8 + Entity Framework Core + SQL Server + Bootstrap 5
- **開發階段**: Phase 2 完成，準備進入測試階段
- **最後更新**: 2025年

---

## ?? 已完成項目

### Phase 1: 資料庫與核心架構 ?

#### 1.1 資料庫腳本
- ? `Database/CreateDatabase.sql` - 建立資料庫與資料表
  - SupplyLocation (據點資料表)
  - SupplyItem (物資資料表)
  - SupplyTransferLog (物資轉移紀錄表)
  - UserAccount (使用者帳號表)
  - 建立索引以提升效能
- ? `Database/InsertTestData.sql` - 插入測試資料
  - 3個測試據點
  - 5筆測試物資

#### 1.2 Models
- ? `Models/SupplyLocation.cs` - 據點模型
- ? `Models/SupplyItem.cs` - 物資模型（含狀態判斷方法）
- ? `Models/SupplyTransferLog.cs` - 轉移紀錄模型
- ? `Models/UserAccount.cs` - 使用者模型

#### 1.3 ViewModels
- ? `Models/ViewModels/DashboardViewModel.cs` - 戰情總覽視圖模型
- ? `Models/ViewModels/MapLocationViewModel.cs` - 地圖視圖模型
- ? `Models/ViewModels/TransferViewModel.cs` - 轉移視圖模型

#### 1.4 Data Layer
- ? `Data/ApplicationDbContext.cs` - EF Core DbContext
  - 設定資料表對應
  - 設定關聯與外鍵
  - 設定 Decimal 精確度

#### 1.5 Services
- ? `Services/DashboardService.cs` - 戰情總覽服務
  - 統計據點數、物資數、庫存數
  - 計算低庫存、即期、過期物資
- ? `Services/SupplyTransferService.cs` - 物資轉移服務
  - 轉移邏輯處理
  - 交易管理（Transaction）
  - 錯誤處理

#### 1.6 Controllers
- ? `Controllers/DashboardController.cs` - 戰情總覽控制器
- ? `Controllers/SupplyLocationController.cs` - 據點管理控制器（完整CRUD）
- ? `Controllers/SupplyItemController.cs` - 物資管理控制器（完整CRUD + 篩選）
- ? `Controllers/SupplyTransferController.cs` - 物資轉移控制器
- ? `Controllers/MapController.cs` - 地圖控制器（提供JSON API）

#### 1.7 Views - 共用
- ? `Views/Shared/_Layout.cshtml` - 主版面配置
  - 中文化導覽列
  - Bootstrap Icons 整合
  - 訊息通知區域（SuccessMessage / ErrorMessage）

#### 1.8 Views - Dashboard
- ? `Views/Dashboard/Index.cshtml` - 戰情總覽頁面
  - 6個統計卡片
  - 據點物資統計表
  - 低庫存物資列表
  - 即將過期物資列表

#### 1.9 Views - Map
- ? `Views/Map/Index.cshtml` - 據點地圖頁面
  - Leaflet.js 整合
  - OpenStreetMap 圖資
  - 據點標記（Marker）
  - Popup 顯示據點摘要

#### 1.10 Views - SupplyLocation ?
- ? `Views/SupplyLocation/Index.cshtml` - 據點列表
- ? `Views/SupplyLocation/Create.cshtml` - 新增據點
- ? `Views/SupplyLocation/Edit.cshtml` - 編輯據點
- ? `Views/SupplyLocation/Details.cshtml` - 據點詳細資料
- ? `Views/SupplyLocation/Delete.cshtml` - 刪除確認

#### 1.11 Views - SupplyItem ?
- ? `Views/SupplyItem/Index.cshtml` - 物資列表（含篩選功能）
- ? `Views/SupplyItem/Create.cshtml` - 新增物資
- ? `Views/SupplyItem/Edit.cshtml` - 編輯物資
- ? `Views/SupplyItem/Details.cshtml` - 物資詳細資料
- ? `Views/SupplyItem/Delete.cshtml` - 刪除確認

#### 1.12 Views - SupplyTransfer ?
- ? `Views/SupplyTransfer/Index.cshtml` - 轉移紀錄列表
- ? `Views/SupplyTransfer/Create.cshtml` - 建立物資轉移

#### 1.13 專案設定
- ? `ResourceSharingPlatform.csproj` - 加入 EF Core 套件
- ? `appsettings.json` - 設定連線字串
- ? `Program.cs` - 設定 DI、DbContext、預設路由

#### 1.14 文件
- ? `Markdown/ResourceSharingPlatform_dev_spec.md` - 開發規格書
- ? `Markdown/DevelopmentProgress.md` - 開發進度追蹤
- ? `Markdown/ExecutionGuide.md` - 執行步驟指南

---

## ?? Phase 2: 完成所有 CRUD Views ?

所有 Views 已完成！

---

## ?? Phase 3: 測試與驗證（當前階段）

### 3.1 資料庫建置
- ? 執行 `Database/CreateDatabase.sql`
- ? 執行 `Database/InsertTestData.sql`
- ? 驗證資料表建立成功
- ? 驗證測試資料載入成功

### 3.2 專案建置測試
- ? 還原 NuGet 套件
- ? 建置專案（確認無編譯錯誤）
- ? 執行專案

### 3.3 功能驗證測試
詳細測試項目請參考 `Markdown/ExecutionGuide.md`

#### 戰情總覽
- ? 顯示統計卡片
- ? 顯示據點統計
- ? 顯示低庫存物資
- ? 顯示即期物資

#### 據點管理
- ? 列表顯示
- ? 新增據點
- ? 編輯據點
- ? 查看詳細資料
- ? 軟刪除據點

#### 物資管理
- ? 列表顯示
- ? 依據點篩選
- ? 依種類篩選
- ? 新增物資
- ? 編輯物資
- ? 查看詳細資料
- ? 狀態顯示（正常/低庫存/即期/過期）
- ? 軟刪除物資

#### 物資轉移
- ? 建立轉移
- ? 數量驗證
- ? 來源數量扣除
- ? 目標數量增加
- ? 轉移紀錄建立
- ? 交易完整性

#### 轉移紀錄
- ? 顯示紀錄列表
- ? 顯示完整資訊

#### 地圖總覽
- ? 地圖載入
- ? 顯示據點標記
- ? Popup 顯示
- ? 連結跳轉

---

## ?? Phase 4: 進階功能（選配）

- ? 使用者登入功能
- ? 角色權限管理
- ? Excel 匯入/匯出
- ? 操作紀錄 Audit Log
- ? 通知功能（Email / LINE Notify）
- ? RWD 手機版優化
- ? 資料備份機制
- ? Azure 部署準備

---

## ?? 立即執行步驟

### 步驟 1：資料庫建置
1. 開啟 SQL Server Management Studio (SSMS)
2. 連線到您的 SQL Server
3. 執行 `Database/CreateDatabase.sql`
4. 執行 `Database/InsertTestData.sql`

### 步驟 2：還原套件
```powershell
cd ResourceSharingPlatform
dotnet restore
```

### 步驟 3：建置專案
```powershell
dotnet build
```

### 步驟 4：執行專案
在 Visual Studio 2022 中按 `F5` 或執行：
```powershell
dotnet run
```

### 步驟 5：開啟瀏覽器
預設首頁：`https://localhost:xxxx/Dashboard`

**詳細步驟請參考：`Markdown/ExecutionGuide.md`**

---

## ?? 環境需求

### 開發環境
- ? Visual Studio 2022
- ? .NET 8 SDK
- ? SQL Server (LocalDB 或 Express 或完整版)
- ? SQL Server Management Studio (SSMS)

### 套件依賴
- ? Microsoft.EntityFrameworkCore.SqlServer (8.0.0)
- ? Microsoft.EntityFrameworkCore.Tools (8.0.0)
- ? Bootstrap 5
- ? Bootstrap Icons
- ? Leaflet.js 1.9.4
- ? OpenStreetMap

---

## ?? 設定說明

### 資料庫連線字串
位置：`appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=LocalSupplyDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

根據您的環境可能需要調整：
- SQL Server Express: `Server=.\\SQLEXPRESS;...`
- LocalDB: `Server=(localdb)\\MSSQLLocalDB;...`
- 具名實例: `Server=YOUR_SERVER_NAME;...`

### 預設首頁
已設定為 `Dashboard/Index`（戰情總覽）

---

## ?? 功能完成度

### 核心功能
- [x] 戰情總覽 Dashboard - 100%
- [x] 據點管理 CRUD - 100%
- [x] 物資管理 CRUD - 100%
- [x] 物資轉移功能 - 100%
- [x] 轉移紀錄查詢 - 100%
- [x] 地圖總覽 - 100%

### 進度總覽
- **Phase 1（資料庫與核心架構）**: ? 100%
- **Phase 2（所有 Views）**: ? 100%
- **Phase 3（測試與驗證）**: ? 0%
- **Phase 4（進階功能）**: ? 0%

**整體完成度：約 90%** （核心功能全部完成，待測試驗證）

---

## ?? 程式碼統計

### 檔案數量
- 資料庫腳本：2 個
- Models：7 個
- Controllers：5 個
- Services：2 個
- Views：19 個
- 文件：3 個

### 程式碼行數（估計）
- 後端 C#：約 2,500 行
- 前端 Razor/HTML：約 2,000 行
- SQL 腳本：約 200 行
- 文件：約 1,500 行

---

## ?? 下一步行動

1. **立即執行**：
   - 按照 `ExecutionGuide.md` 步驟建立資料庫
   - 執行專案並進行基本測試

2. **功能驗證**：
   - 測試所有 CRUD 功能
   - 測試物資轉移邏輯
   - 測試地圖顯示

3. **錯誤修正**：
   - 記錄任何發現的問題
   - 修正 bugs
   - 優化使用者體驗

4. **選配功能**（依需求）：
   - 實作登入功能
   - 加入 Excel 匯入/匯出
   - 準備 Azure 部署

---

## ?? 已知問題
目前無已知問題（待測試驗證）

---

## ?? 技術支援

### 相關文件
- 開發規格：`Markdown/ResourceSharingPlatform_dev_spec.md`
- 執行指南：`Markdown/ExecutionGuide.md`
- 進度追蹤：`Markdown/DevelopmentProgress.md`

### 常見問題
請參考 `ExecutionGuide.md` 中的「常見問題排除」章節

---

## ? 驗收標準

專案可以正式交付使用需滿足：
- [x] 所有程式碼編寫完成
- [ ] 資料庫成功建立並載入測試資料
- [ ] 專案可以成功建置
- [ ] 專案可以成功執行
- [ ] 所有 CRUD 功能正常運作
- [ ] 物資轉移功能正常運作
- [ ] 地圖顯示正常
- [ ] 戰情總覽數據正確
- [ ] 無重大 bugs

**目前狀態：已完成所有程式碼，準備進入測試階段！** ??

---

## Phase 5：權限系統與功能擴充

### 5.1 權限管理
- 新增登入／登出（Cookie Authentication）、角色設計（管理人員／幹部／社工，`Models/Roles.cs`）
- 預設系統管理員帳號 `admin` / `admin`（首次啟動自動建立，密碼已雜湊儲存）
- 帳號管理（`UserAccountController`，僅管理人員可用）
- 既有 CRUD／轉移功能依角色加上 `[Authorize]` 限制

### 5.2 物資出庫（發放）
- 新增 `SupplyOutboundLog`、`SupplyOutboundController`、`SupplyOutboundService`
- 出庫會扣減庫存並記錄領用人姓名／聯絡方式／操作人員

### 5.3 物資規格／圖片／分類管理
- `SupplyItem` 新增 `Specification`（規格）、`ImagePath`（圖片，存放於 `wwwroot/uploads/items/`）、`StockType`（無效期物資／有效期物資／冷凍食品）
- 新增／編輯表單重新設計為分類按鈕＋動態欄位顯示；Index 頁新增分類快速篩選

### 5.4 出庫即期警示
- 出庫選單依效期排序（即期優先），並在「新增出庫」「出庫紀錄」頁面加上即期／已過期物資警示區塊

### 5.5 物資轉移：批次轉移＋到貨確認
- 轉移表單支援一次新增多筆物資（共用 `BatchId`）
- 新增 `Pending`／`Confirmed`／`Cancelled` 狀態機：建立轉移先扣來源庫存，需經「確認送達」才會加進目標據點庫存，可「取消」退回來源庫存

詳細設計請見 `Markdown/ResourceSharingPlatform_dev_spec.md` 第 30 節。

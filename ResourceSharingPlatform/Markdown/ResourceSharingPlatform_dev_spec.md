# 地方物資管理平台－開發規格書

最後更新：2026-08-07

## 1. 系統定位

本系統管理多個地方據點的物資主檔、庫存、效期、圖片與異動紀錄，並提供兩層安全庫存警示。

## 2. 技術架構

```text
Browser
  → ASP.NET Core MVC Controller
  → ViewModel / Service
  → Entity Framework Core DbContext
  → SQL Server LocalSupplyDB
  → Razor View / JSON
```

- Framework：.NET 8
- UI：Razor Views、Bootstrap 5、Bootstrap Icons
- ORM：EF Core 8
- Authentication：Cookie
- Database：SQL Server
- Excel：ClosedXML
- Map：Leaflet + OpenStreetMap

## 3. 分層責任

- `Controllers`：HTTP、表單驗證、權限與畫面流程。
- `Services`：Dashboard、調撥、出庫、捐贈、報廢與 AI 入庫邏輯。
- `Models`：EF Core 實體及狀態常數。
- `Models/ViewModels`：表單、統計與顯示模型。
- `Data/ApplicationDbContext`：資料表映射、關聯與索引。
- `Data/DbInitializer`：必要設定列、舊資料遷移與啟動資料整理。
- `Views`：Razor UI。
- `wwwroot/uploads`：圖片實體檔案。

## 4. 角色與權限

| 角色 | 權限摘要 |
|---|---|
| `Admin` | 所有功能、帳號、庫存種類、LINE、AI 設定 |
| `Cadre` | 物資與據點維護、調撥及一般異動 |
| `SocialWorker` | 一般查詢與所屬據點作業 |

未標示 `[AllowAnonymous]` 的頁面預設必須登入。

## 5. 物資主檔規格

### 5.1 物資定義

`InventoryItemDefinition` 以 `Category + ItemName` 表示一種物資，保存：

- 最小計算單位 `Unit`
- 全系統安全庫存 `GlobalSafetyStock`
- 庫存分類 `StockType`（`NoExpiry`／`HasExpiry`／`Frozen`，跟 `SupplyItem.StockType` 同一組常數）
- 啟用狀態

啟用中的種類與名稱不可重複。

`StockType` 由「系統管理 → 庫存種類設定」的編輯頁面維護，「新增物資」頁面的物資種類/名稱/規格三個下拉選單會依目前選取的分類（無效期物資/有效期物資/冷凍食品）篩選，切換分類時已選的內容會清空重選，因為可選範圍改變了。既有資料庫升級時，`StockType` 會依該物資底下 `SupplyItem` 實際使用最多次的分類自動帶入一次；一旦透過編輯頁面手動存過檔（`UpdatedAt` 不為 NULL），之後開機就不會再自動覆蓋。

### 5.2 規格

`InventoryItemVariant` 保存同一物資的規格，例如 600ml、L、XL。規格不影響安全庫存分組。

### 5.3 據點門檻

`LocationInventorySafetyStock` 以 `LocationId + InventoryItemDefinitionId` 唯一，保存該據點的安全庫存。

## 6. 安全庫存規則

### 據點低庫存

```text
SUM(SupplyItem.Quantity)
GROUP BY LocationId, Category, ItemName
```

合計小於或等於該據點門檻時顯示警示；規格、效期與庫存類型不拆分門檻。

### 全系統總量不足

```text
SUM(SupplyItem.Quantity)
GROUP BY Category, ItemName
```

只計算各據點已存在的實際庫存，不包含 `Pending` 調撥數量。合計小於或等於 `GlobalSafetyStock` 時顯示警示。

### 有效期限

- 已過期：`ExpirationDate < Today`
- 即將過期：`Today <= ExpirationDate <= Today + 30 days`
- 期限按個別 `SupplyItem` 判斷，不因安全庫存合併。

## 7. 庫存異動

### 調撥

1. 建立 `Pending` 時扣來源庫存。
2. 確認收貨時增加目的庫存並改為 `Confirmed`。
3. 取消時退回來源庫存並改為 `Cancelled`。
4. 新建目的庫存時沿用規格外鍵，安全庫存取目的據點門檻。

### 出庫

扣減庫存並新增 `SupplyOutboundLog`，不得超過目前庫存。

### 捐贈

增加既有物資庫存並新增 `SupplyDonationLog`。

### 報廢

扣減庫存並新增 `SupplyDisposalLog`，原因為 Expired、Damaged、Lost 或 Other。

所有庫存異動使用資料庫 Transaction。

## 8. 圖片

- 允許 jpg、jpeg、png、webp。
- 單檔上限 5 MB。
- 實際存放路徑由 `appsettings.json` 的 `UploadsRoot`（`Services/UploadPathProvider.cs`）決定，未設定時退回 `wwwroot/uploads`；一律以 `/uploads/...` 這個相對路徑對外提供，SQL 也只保存這個相對路徑。
  - 一般物資圖片子資料夾：`items`
  - AI 智慧入庫圖片子資料夾：`ai-stockin`
- **一般物資圖片命名規則**（`SupplyItemController.SaveImageAsync`）：`物資種類-物資名稱-規格-數量-日期-流水號`，例如 `食品-飲用水-600ml-250-20260809-001.png`。自由文字欄位（種類/名稱/規格）會先過濾掉 Windows 檔名不允許的字元；流水號從同前綴的既有檔案中找最大值+1，避免同一天同一物資重複上傳互相覆蓋。
  - AI 智慧入庫的輸入照片（拍照上傳當下還不知道正確品項）維持 GUID 命名，不套用這個規則。

## 9. 權限與資料範圍

- Cookie Claims：UserId、UserName、Role、DisplayName、LocationId。
- Admin 可跨據點。
- 一般據點使用者依 `LocationId` 限制作業範圍。
- 轉移確認與取消由目的據點或 Admin 處理。

## 10. 資料庫版本策略

- 全新建庫：`Database/CreateDatabase.sql`
- 舊版升級：`DbInitializer.EnsureInventoryCatalogTablesAsync`（原名 `EnsureInventoryTypeSettingTableAsync`，2026-08 隨 10.1 的移除一併改名，改名更貼近它現在實際負責的目錄資料表）
- SQL 必須保持 idempotent。
- 正式升級前必須備份資料庫及 uploads；每週自動備份與還原方式見 `Markdown/BackupPlan.md`。

### 10.1 已移除：InventoryTypeSetting（2026-08）

`InventoryTypeSetting` 是 `InventoryItemDefinition`／`InventoryItemVariant` 這套正式目錄出現前的過渡表，原始設計是「先寫入 InventoryTypeSetting，再遷移進 InventoryItemDefinition」。但這台機器的資料庫從未真的寫入過這張表（0 筆），現有的 `InventoryItemDefinition` 資料是 `DbInitializer.BackfillInventoryDefinitionsFromSupplyItemsAsync` 直接從 `SupplyItem` 回填出來的；除了它自己的 model／`DbInitializer`／`SupplyItem.InventoryTypeSettingId` 這條 FK 外，沒有任何 Controller/Service/View 讀寫它（`InventoryTypeSettingController` 這個名字容易誤會，但它從頭到尾操作的都是 `InventoryItemDefinition`，跟這張舊表無關）。

確認是乾淨的死資料後移除：`Models/InventoryTypeSetting.cs`、`SupplyItem.InventoryTypeSettingId` 欄位與導覽屬性、`ApplicationDbContext` 的對應設定、`Database/CreateDatabase.sql` 的建表語句都已拿掉。既有安裝升級時，`EnsureInventoryCatalogTablesAsync` 會自動偵測並 DROP 掉殘留的表／欄位／FK／索引（`IF EXISTS` 包裹，全新資料庫是 no-op）。`InventoryTypeSettingController` 與 `Views/InventoryTypeSetting/*` 維持不動——路由名稱雖然容易誤會，但改名會動到 `/InventoryTypeSetting/...` 這個既有路由與導覽列連結，屬於單純改名不算這次重構範圍。

這次重構刻意不包含拔掉 `SupplyItem.Category/ItemName/Specification/Unit/StockType/SafetyStock` 這些跟目錄有重疊的欄位——它們被 14 個以上的既有功能（出庫/捐贈/報廢/轉移/戰情總覽/據點地圖/AI入庫等）直接讀寫，且本質上是「建立當下的目錄快照」，對物資異動紀錄有其歷史留存價值（例如日後改了種類名稱，舊紀錄仍保留當時的名稱），不算真正的技術債，因此保留。

## 11. 已知限制

- 尚未使用 EF Core Migrations。
- `SupplyItem.Quantity` 是可變餘額，尚未建立統一 InventoryTransaction Ledger。
- `SupplyItem.SafetyStock` 為建立當下的據點門檻快照，新警示邏輯不以它為主（實際判斷邏輯見第 6 節）。
- LINE 與 AI 外部 API 尚未正式串接。
- API 金鑰資料表目前仍可存明文，正式環境需改用 Secret Store。
- `SupplyItem` 尚未加入 rowversion 併發控制。

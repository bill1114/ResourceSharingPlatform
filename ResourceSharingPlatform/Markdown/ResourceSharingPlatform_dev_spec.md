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
- 啟用狀態

啟用中的種類與名稱不可重複。

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
- 檔名使用 GUID。
- 一般圖片：`wwwroot/uploads/items`
- AI 圖片：`wwwroot/uploads/ai-stockin`
- SQL 只保存相對路徑。

## 9. 權限與資料範圍

- Cookie Claims：UserId、UserName、Role、DisplayName、LocationId。
- Admin 可跨據點。
- 一般據點使用者依 `LocationId` 限制作業範圍。
- 轉移確認與取消由目的據點或 Admin 處理。

## 10. 資料庫版本策略

- 全新建庫：`Database/CreateDatabase.sql`
- 舊版升級：`DbInitializer.EnsureInventoryTypeSettingTableAsync`
- 舊 `InventoryTypeSetting` 只做一次新主檔遷移。
- SQL 必須保持 idempotent。
- 正式升級前必須備份資料庫及 uploads。

## 11. 已知限制

- 尚未使用 EF Core Migrations。
- `SupplyItem.Quantity` 是可變餘額，尚未建立統一 InventoryTransaction Ledger。
- `SupplyItem.SafetyStock` 與 `InventoryTypeSetting` 為相容舊版保留，新警示邏輯不以它們為主。
- LINE 與 AI 外部 API 尚未正式串接。
- API 金鑰資料表目前仍可存明文，正式環境需改用 Secret Store。
- `SupplyItem` 尚未加入 rowversion 併發控制。

# 地方物資管理平台－完成報告

最後更新：2026-08-09

## 專案狀態

目前版本已可建置、啟動並連接 SQL Server `LocalSupplyDB`，定位為可供內部驗收與部署測試的版本。

- ASP.NET Core MVC / .NET 8
- Entity Framework Core 8 / SQL Server
- Bootstrap 5 / Razor Views
- Cookie Authentication 與角色授權
- Release 與 Debug 均應由專案根目錄建置

## 已完成功能

1. 使用者登入、登出、帳號管理與角色權限。
2. 據點新增、編輯、停用、詳細資料與地圖顯示。
3. 庫存種類主檔、規格、最小單位、全系統安全庫存與各據點安全庫存。
4. 物資新增、編輯、停用、圖片上傳、查詢與 Excel 匯出。
5. 據點間批次調撥、待收貨、確認收貨與取消。
6. 出庫、捐贈、報廢及其歷史紀錄與 Excel 匯出。
7. 戰情總覽、據點低庫存、全系統總量不足、即期與過期警示。
8. 領取者分析。
9. LINE 通知設定介面與測試模擬。
10. AI 智慧入庫設定、輸入、確認與紀錄介面。

## 安全庫存規則

- 規格不參與安全庫存分組。
- 據點安全庫存以 `LocationId + Category + ItemName` 的實際庫存合計判斷。
- 全系統安全庫存以 `Category + ItemName` 在所有據點的實際庫存合計判斷。
- 「目前數量」不包含尚未確認收貨的調撥數量。
- 有效期限仍按每一筆庫存批次判斷。
- 安全庫存為 0 時視為未啟用該項警示。

## 資料升級狀態

物資目錄使用：

- `InventoryItemDefinition`
- `InventoryItemVariant`
- `LocationInventorySafetyStock`

新增物資使用 `SupplyItem.InventoryItemVariantId`。過渡表 `InventoryTypeSetting` 與 `SupplyItem.InventoryTypeSettingId` 已於 2026-08 確認為死資料後移除，既有安裝升級時會自動清除殘留物件（詳見 `ResourceSharingPlatform_dev_spec.md` 10.1）。

## 檔案儲存

- 一般物資圖片：`wwwroot/uploads/items`
- AI 入庫圖片：`wwwroot/uploads/ai-stockin`
- 資料庫只保存相對路徑，不保存圖片二進位資料。

部署時必須將 uploads 資料夾視為持久化資料並納入備份。

## 驗證結果

- 專案建置：0 warning / 0 error
- 新安全庫存資料表已在 `LocalSupplyDB` 建立
- 舊設定、規格、據點門檻與既有物資外鍵已完成遷移驗證

## 尚未完成或需正式環境補強

- LINE Messaging API 目前只有設定與模擬測試，尚未正式推播。
- AI 入庫尚未串接正式模型 API。
- 圖片目前保存在網站目錄，正式部署建議改用持久化磁碟、NAS 或物件儲存。
- 專案尚未導入 EF Core Migrations，資料庫版本由 SQL 與啟動初始化程式共同維護。
- 正式上線前必須更換預設管理員密碼、連線字串與所有 API 金鑰。

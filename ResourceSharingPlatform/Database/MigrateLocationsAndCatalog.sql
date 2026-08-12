-- MigrateLocationsAndCatalog.sql
-- 把「這台機器」目前實際生效的「據點資料」(SupplyLocation) 與「庫存種類」
-- (InventoryItemDefinition + InventoryItemVariant) 搬移到新機器的 LocalSupplyDB。
--
-- 資料是這次直接查詢目前資料庫的即時內容產生的（2026-08-10 匯出），不是套用預設模板，
-- 所以包含你在系統管理畫面裡手動調整過的內容（例如「手電筒」的安全庫存 100、
-- 「食品/水」沿用舊測試資料的單位「瓶」與分類「冷凍食品」）。
--
-- 使用方式：在新機器上，資料庫（LocalSupplyDB）建立好之後（跑過 CreateDatabase.sql），
-- 直接執行本檔案即可：
--   sqlcmd -S . -E -d LocalSupplyDB -i MigrateLocationsAndCatalog.sql
--
-- 冪等（idempotent）：可以重複執行，已存在的據點名稱／物資種類名稱／規格不會被
-- 重複插入，也不會覆蓋新機器上已經存在、可能已經手動改過的資料。

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- ========== 1. 據點資料 (SupplyLocation) ==========

DECLARE @NewLocations TABLE (LocationName NVARCHAR(200), Address NVARCHAR(400), Phone NVARCHAR(60), Latitude DECIMAL(10,7), Longitude DECIMAL(10,7));
INSERT INTO @NewLocations (LocationName, Address, Phone, Latitude, Longitude) VALUES
(N'行政中心', N'雲林縣斗六市府文路30號', N'05-5341940', 23.6960101, 120.5278435),
(N'圓夢庇護工場', N'雲林縣斗六市保長路504號', N'05-5345467', 23.7059203, 120.5189915),
(N'雲林縣身心障礙者服務中心-斗六區', N'雲林縣斗六市府文路22號4樓', N'05-5362103', 23.6960101, 120.5278435),
(N'心歡喜日照中心', N'雲林縣斗六市南京路373號1樓', N'05-5372781', 23.7138970, 120.5393339),
(N'西螺服務據點', N'雲林縣西螺鎮光復西路286號', N'05-5873733', 23.7966480, 120.4594180),
(N'東勢服務中心', N'雲林縣東勢鄉東北村東勢東路395號', N'05-6993809', 23.6758407, 120.2618627),
(N'心圓寶日照中心', N'雲林縣北港鎮新街里穎寧街72號', N'05-7825113', 23.5788243, 120.2964091),
(N'北港服務中心', N'雲林縣北港鎮新街里新東街33巷8之3號', N'05-7827433', 23.5853932, 120.3010845);

INSERT INTO SupplyLocation (LocationName, Address, Phone, Latitude, Longitude, IsActive, CreatedAt)
SELECT nl.LocationName, nl.Address, nl.Phone, nl.Latitude, nl.Longitude, 1, GETDATE()
FROM @NewLocations nl
WHERE NOT EXISTS (
    SELECT 1 FROM SupplyLocation sl WHERE sl.LocationName = nl.LocationName
);

PRINT N'據點資料處理完成。';

-- ========== 2. 庫存種類目錄 (InventoryItemDefinition) ==========

DECLARE @NewDefinitions TABLE (Category NVARCHAR(50), ItemName NVARCHAR(100), Unit NVARCHAR(20), StockType NVARCHAR(20), GlobalSafetyStock INT);
INSERT INTO @NewDefinitions (Category, ItemName, Unit, StockType, GlobalSafetyStock) VALUES
(N'防護', N'口罩', N'片', N'HasExpiry', 0),
(N'食品', N'引用水', N'瓶', N'HasExpiry', 0),
(N'工具', N'手電筒', N'支', N'HasExpiry', 100),
(N'生活', N'毛毯', N'件', N'HasExpiry', 0),
(N'食品', N'水', N'瓶', N'Frozen', 0),
(N'食品', N'泡麵', N'箱', N'HasExpiry', 0),
(N'醫療', N'急救包', N'組', N'HasExpiry', 0),
(N'食品', N'飲用水', N'瓶', N'HasExpiry', 0),
(N'生活', N'輔具', N'件', N'NoExpiry', 0),
(N'食品', N'礦泉水(AI測試)', N'箱', N'NoExpiry', 0),
(N'食品', N'米', N'包', N'HasExpiry', 0),
(N'食品', N'米酒', N'瓶', N'HasExpiry', 0),
(N'食品', N'食用油', N'瓶', N'HasExpiry', 0),
(N'食品', N'醬油', N'瓶', N'HasExpiry', 0),
(N'食品', N'飲料', N'鋁箔包', N'HasExpiry', 0),
(N'食品', N'罐頭', N'罐', N'HasExpiry', 0),
(N'食品', N'綠豆', N'包', N'HasExpiry', 0),
(N'食品', N'鹽巴', N'包', N'HasExpiry', 0),
(N'食品', N'冬粉', N'包', N'HasExpiry', 0),
(N'食品', N'米粉', N'包', N'HasExpiry', 0),
(N'食品', N'麵條', N'包', N'HasExpiry', 0),
(N'食品', N'泡麵(袋裝)', N'包', N'HasExpiry', 0),
(N'食品', N'泡麵(碗裝)', N'碗', N'HasExpiry', 0),
(N'生鮮冷凍食品', N'蔬菜', N'箱', N'Frozen', 0),
(N'生鮮冷凍食品', N'豬肉', N'包', N'Frozen', 0),
(N'生鮮冷凍食品', N'牛肉', N'包', N'Frozen', 0),
(N'生鮮冷凍食品', N'雞肉', N'包', N'Frozen', 0),
(N'生鮮冷凍食品', N'雞蛋(盒裝)', N'盒', N'HasExpiry', 0),
(N'生鮮冷凍食品', N'雞蛋(箱裝)', N'箱', N'HasExpiry', 0),
(N'生鮮冷凍食品', N'甜點類', N'包', N'Frozen', 0),
(N'生鮮冷凍食品', N'湯類', N'包', N'Frozen', 0),
(N'生鮮冷凍食品', N'沖泡類', N'包', N'HasExpiry', 0),
(N'生鮮冷凍食品', N'麵包類', N'個', N'Frozen', 0),
(N'日用品', N'成人紙尿布', N'包', N'NoExpiry', 0),
(N'日用品', N'尿布墊', N'包', N'NoExpiry', 0),
(N'日用品', N'棉被', N'條', N'NoExpiry', 0),
(N'日用品', N'毯子', N'條', N'NoExpiry', 0),
(N'日用品', N'床墊', N'座', N'NoExpiry', 0),
(N'輔具', N'一般輪椅', N'台', N'NoExpiry', 0),
(N'輔具', N'鐵製輪椅', N'台', N'NoExpiry', 0),
(N'輔具', N'輕便輪椅', N'台', N'NoExpiry', 0),
(N'輔具', N'高背輪椅', N'台', N'NoExpiry', 0),
(N'輔具', N'便盆椅', N'座', N'NoExpiry', 0),
(N'輔具', N'氣墊床', N'床', N'NoExpiry', 0),
(N'輔具', N'電動床', N'座', N'NoExpiry', 0),
(N'輔具', N'單拐', N'隻', N'NoExpiry', 0),
(N'輔具', N'雙枴', N'隻', N'NoExpiry', 0),
(N'輔具', N'四腳拐', N'組', N'NoExpiry', 0);

INSERT INTO InventoryItemDefinition (Category, ItemName, Unit, GlobalSafetyStock, IsActive, CreatedAt, StockType)
SELECT nd.Category, nd.ItemName, nd.Unit, nd.GlobalSafetyStock, 1, GETDATE(), nd.StockType
FROM @NewDefinitions nd
WHERE NOT EXISTS (
    SELECT 1 FROM InventoryItemDefinition d
    WHERE d.Category = nd.Category AND d.ItemName = nd.ItemName AND d.IsActive = 1
);

PRINT N'庫存種類目錄處理完成。';

-- ========== 3. 物資規格 (InventoryItemVariant) ==========

DECLARE @NewVariants TABLE (Category NVARCHAR(50), ItemName NVARCHAR(100), Specification NVARCHAR(200));
INSERT INTO @NewVariants (Category, ItemName, Specification) VALUES
(N'防護', N'口罩', NULL),
(N'食品', N'引用水', N'600ml'),
(N'工具', N'手電筒', N'LED 大型'),
(N'生活', N'毛毯', NULL),
(N'食品', N'水', N'600ml'),
(N'食品', N'水', N'300ML'),
(N'食品', N'泡麵', NULL),
(N'醫療', N'急救包', NULL),
(N'食品', N'飲用水', NULL),
(N'生活', N'輔具', N'輪椅'),
(N'食品', N'礦泉水(AI測試)', NULL),
(N'食品', N'米', N'1公斤'),
(N'食品', N'米', N'3公斤'),
(N'食品', N'米', N'5公斤'),
(N'食品', N'米', N'30公斤'),
(N'食品', N'米酒', N'600ML'),
(N'食品', N'食用油', N'600ML'),
(N'食品', N'醬油', N'600ML'),
(N'食品', N'飲料', N'300ML'),
(N'食品', N'飲料', N'600ML'),
(N'食品', N'飲料', N'975ML'),
(N'食品', N'罐頭', N'八寶粥類'),
(N'食品', N'罐頭', N'魚類'),
(N'食品', N'罐頭', N'醬瓜類'),
(N'食品', N'綠豆', N'無'),
(N'食品', N'鹽巴', N'無'),
(N'食品', N'冬粉', N'無'),
(N'食品', N'米粉', N'無'),
(N'食品', N'麵條', N'無'),
(N'食品', N'泡麵(袋裝)', N'無'),
(N'食品', N'泡麵(碗裝)', N'無'),
(N'生鮮冷凍食品', N'蔬菜', N'無'),
(N'生鮮冷凍食品', N'豬肉', N'無'),
(N'生鮮冷凍食品', N'牛肉', N'無'),
(N'生鮮冷凍食品', N'雞肉', N'無'),
(N'生鮮冷凍食品', N'雞蛋(盒裝)', N'12入'),
(N'生鮮冷凍食品', N'雞蛋(箱裝)', N'無'),
(N'生鮮冷凍食品', N'甜點類', N'無'),
(N'生鮮冷凍食品', N'湯類', N'無'),
(N'生鮮冷凍食品', N'沖泡類', N'無'),
(N'生鮮冷凍食品', N'麵包類', N'無'),
(N'日用品', N'成人紙尿布', N'S'),
(N'日用品', N'成人紙尿布', N'M'),
(N'日用品', N'成人紙尿布', N'L'),
(N'日用品', N'成人紙尿布', N'XL'),
(N'日用品', N'尿布墊', N'S'),
(N'日用品', N'尿布墊', N'M'),
(N'日用品', N'尿布墊', N'L'),
(N'日用品', N'尿布墊', N'XL'),
(N'日用品', N'棉被', N'無'),
(N'日用品', N'毯子', N'無'),
(N'日用品', N'床墊', N'單人'),
(N'日用品', N'床墊', N'雙人'),
(N'輔具', N'一般輪椅', N'無'),
(N'輔具', N'鐵製輪椅', N'無'),
(N'輔具', N'輕便輪椅', N'無'),
(N'輔具', N'高背輪椅', N'無'),
(N'輔具', N'便盆椅', N'無'),
(N'輔具', N'氣墊床', N'無'),
(N'輔具', N'電動床', N'無'),
(N'輔具', N'單拐', N'無'),
(N'輔具', N'雙枴', N'無'),
(N'輔具', N'四腳拐', N'無');

INSERT INTO InventoryItemVariant (InventoryItemDefinitionId, Specification, IsActive, CreatedAt)
SELECT d.Id, nv.Specification, 1, GETDATE()
FROM @NewVariants nv
INNER JOIN InventoryItemDefinition d
    ON d.Category = nv.Category AND d.ItemName = nv.ItemName AND d.IsActive = 1
WHERE NOT EXISTS (
    SELECT 1 FROM InventoryItemVariant v
    WHERE v.InventoryItemDefinitionId = d.Id
      AND (v.Specification = nv.Specification OR (v.Specification IS NULL AND nv.Specification IS NULL))
      AND v.IsActive = 1
);

PRINT N'物資規格處理完成。';

-- ========== 4. 結果確認 ==========

SELECT '據點資料' AS 項目, COUNT(*) AS 目前筆數 FROM SupplyLocation WHERE IsActive = 1
UNION ALL
SELECT '庫存種類（物資名稱）', COUNT(*) FROM InventoryItemDefinition WHERE IsActive = 1
UNION ALL
SELECT '物資規格', COUNT(*) FROM InventoryItemVariant WHERE IsActive = 1;

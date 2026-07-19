-- ================================================================
-- 地方物資管理平台 - 測試資料
-- Description: 插入初始測試資料
-- ================================================================

USE LocalSupplyDB;
GO

-- ================================================================
-- 1. 插入測試據點資料
-- ================================================================
SET IDENTITY_INSERT SupplyLocation ON;

IF NOT EXISTS (SELECT * FROM SupplyLocation WHERE Id = 1)
BEGIN
    INSERT INTO SupplyLocation (Id, LocationName, Address, Latitude, Longitude, ContactPerson, Phone, IsActive, CreatedAt)
    VALUES
    (1, N'第一物資據點', N'雲林縣斗六市', 23.7078, 120.5439, N'王先生', N'05-0000001', 1, GETDATE()),
    (2, N'第二物資據點', N'雲林縣虎尾鎮', 23.7092, 120.4313, N'陳小姐', N'05-0000002', 1, GETDATE()),
    (3, N'第三物資據點', N'雲林縣西螺鎮', 23.8000, 120.4600, N'林先生', N'05-0000003', 1, GETDATE());
    
    PRINT '據點測試資料插入完成！';
END
ELSE
BEGIN
    PRINT '據點測試資料已存在，跳過插入。';
END

SET IDENTITY_INSERT SupplyLocation OFF;
GO

-- ================================================================
-- 2. 插入測試物資資料
-- ================================================================
SET IDENTITY_INSERT SupplyItem ON;

IF NOT EXISTS (SELECT * FROM SupplyItem WHERE Id = 1)
BEGIN
    INSERT INTO SupplyItem (Id, Category, ItemName, Quantity, Unit, ExpirationDate, LocationId, SafetyStock, Remark, IsActive, CreatedAt)
    VALUES
    (1, N'食品', N'飲用水', 500, N'瓶', '2026-12-31', 1, 100, N'箱裝飲用水', 1, GETDATE()),
    (2, N'食品', N'泡麵', 120, N'箱', '2026-08-31', 1, 50, N'緊急糧食', 1, GETDATE()),
    (3, N'醫療', N'急救包', 30, N'組', '2027-01-31', 2, 20, N'基本急救用品', 1, GETDATE()),
    (4, N'防護', N'口罩', 2000, N'片', '2026-06-30', 2, 500, N'一般醫療口罩', 1, GETDATE()),
    (5, N'生活', N'毛毯', 80, N'件', NULL, 3, 30, N'保暖用品', 1, GETDATE());
    
    PRINT '物資測試資料插入完成！';
END
ELSE
BEGIN
    PRINT '物資測試資料已存在，跳過插入。';
END

SET IDENTITY_INSERT SupplyItem OFF;
GO

-- ================================================================
-- 3. 查詢驗證
-- ================================================================
PRINT '=== 據點資料 ===';
SELECT * FROM SupplyLocation;

PRINT '=== 物資資料 ===';
SELECT 
    si.Id,
    si.Category,
    si.ItemName,
    si.Quantity,
    si.Unit,
    si.ExpirationDate,
    sl.LocationName,
    si.SafetyStock,
    si.Remark
FROM SupplyItem si
INNER JOIN SupplyLocation sl ON si.LocationId = sl.Id;

PRINT '測試資料載入完成！';
GO

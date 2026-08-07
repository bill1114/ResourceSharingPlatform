-- 地方物資管理平台 - 測試資料

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO
-- 請先執行 CreateDatabase.sql

USE LocalSupplyDB;
GO

IF NOT EXISTS (SELECT 1 FROM SupplyLocation)
BEGIN
    INSERT INTO SupplyLocation
        (LocationName, Address, Latitude, Longitude, ContactPerson, Phone)
    VALUES
        (N'第一物資據點', N'台北市中正區測試路1號', 25.0330000, 121.5654000, N'王小明', N'02-11111111'),
        (N'第二物資據點', N'新北市板橋區測試路2號', 25.0114000, 121.4618000, N'李小華', N'02-22222222'),
        (N'第三物資據點', N'桃園市桃園區測試路3號', 24.9937000, 121.3010000, N'陳美玲', N'03-33333333');
END
GO

IF NOT EXISTS (SELECT 1 FROM InventoryItemDefinition)
BEGIN
    INSERT INTO InventoryItemDefinition
        (Category, ItemName, Unit, GlobalSafetyStock)
    VALUES
        (N'食品', N'飲用水', N'瓶', 1000),
        (N'生活', N'尿布', N'件', 100),
        (N'輔具', N'輪椅', N'件', 10);
END
GO

DECLARE @WaterDefinitionId INT = (SELECT TOP 1 Id FROM InventoryItemDefinition WHERE Category=N'食品' AND ItemName=N'飲用水' AND IsActive=1);
DECLARE @DiaperDefinitionId INT = (SELECT TOP 1 Id FROM InventoryItemDefinition WHERE Category=N'生活' AND ItemName=N'尿布' AND IsActive=1);
DECLARE @WheelchairDefinitionId INT = (SELECT TOP 1 Id FROM InventoryItemDefinition WHERE Category=N'輔具' AND ItemName=N'輪椅' AND IsActive=1);

IF NOT EXISTS (SELECT 1 FROM InventoryItemVariant WHERE InventoryItemDefinitionId=@WaterDefinitionId AND Specification=N'600ml' AND IsActive=1)
    INSERT INTO InventoryItemVariant (InventoryItemDefinitionId, Specification) VALUES (@WaterDefinitionId, N'600ml');
IF NOT EXISTS (SELECT 1 FROM InventoryItemVariant WHERE InventoryItemDefinitionId=@DiaperDefinitionId AND Specification=N'L' AND IsActive=1)
    INSERT INTO InventoryItemVariant (InventoryItemDefinitionId, Specification) VALUES (@DiaperDefinitionId, N'L');
IF NOT EXISTS (SELECT 1 FROM InventoryItemVariant WHERE InventoryItemDefinitionId=@DiaperDefinitionId AND Specification=N'XL' AND IsActive=1)
    INSERT INTO InventoryItemVariant (InventoryItemDefinitionId, Specification) VALUES (@DiaperDefinitionId, N'XL');
IF NOT EXISTS (SELECT 1 FROM InventoryItemVariant WHERE InventoryItemDefinitionId=@WheelchairDefinitionId AND Specification=N'XL' AND IsActive=1)
    INSERT INTO InventoryItemVariant (InventoryItemDefinitionId, Specification) VALUES (@WheelchairDefinitionId, N'XL');

DECLARE @Location1 INT = (SELECT TOP 1 Id FROM SupplyLocation WHERE LocationName=N'第一物資據點');
DECLARE @Location2 INT = (SELECT TOP 1 Id FROM SupplyLocation WHERE LocationName=N'第二物資據點');
DECLARE @Location3 INT = (SELECT TOP 1 Id FROM SupplyLocation WHERE LocationName=N'第三物資據點');

IF NOT EXISTS (SELECT 1 FROM LocationInventorySafetyStock WHERE LocationId=@Location1 AND InventoryItemDefinitionId=@WaterDefinitionId)
    INSERT INTO LocationInventorySafetyStock (LocationId, InventoryItemDefinitionId, SafetyStock) VALUES (@Location1, @WaterDefinitionId, 60);
IF NOT EXISTS (SELECT 1 FROM LocationInventorySafetyStock WHERE LocationId=@Location2 AND InventoryItemDefinitionId=@WaterDefinitionId)
    INSERT INTO LocationInventorySafetyStock (LocationId, InventoryItemDefinitionId, SafetyStock) VALUES (@Location2, @WaterDefinitionId, 60);
IF NOT EXISTS (SELECT 1 FROM LocationInventorySafetyStock WHERE LocationId=@Location1 AND InventoryItemDefinitionId=@DiaperDefinitionId)
    INSERT INTO LocationInventorySafetyStock (LocationId, InventoryItemDefinitionId, SafetyStock) VALUES (@Location1, @DiaperDefinitionId, 100);
IF NOT EXISTS (SELECT 1 FROM LocationInventorySafetyStock WHERE LocationId=@Location3 AND InventoryItemDefinitionId=@DiaperDefinitionId)
    INSERT INTO LocationInventorySafetyStock (LocationId, InventoryItemDefinitionId, SafetyStock) VALUES (@Location3, @DiaperDefinitionId, 10);

DECLARE @WaterVariantId INT = (SELECT TOP 1 Id FROM InventoryItemVariant WHERE InventoryItemDefinitionId=@WaterDefinitionId AND Specification=N'600ml' AND IsActive=1);
DECLARE @DiaperLVariantId INT = (SELECT TOP 1 Id FROM InventoryItemVariant WHERE InventoryItemDefinitionId=@DiaperDefinitionId AND Specification=N'L' AND IsActive=1);
DECLARE @DiaperXLVariantId INT = (SELECT TOP 1 Id FROM InventoryItemVariant WHERE InventoryItemDefinitionId=@DiaperDefinitionId AND Specification=N'XL' AND IsActive=1);

IF NOT EXISTS (SELECT 1 FROM SupplyItem WHERE LocationId=@Location1 AND InventoryItemVariantId=@WaterVariantId AND ExpirationDate='2027-12-31' AND IsActive=1)
BEGIN
    INSERT INTO SupplyItem
        (Category, ItemName, Specification, Quantity, Unit, StockType, ExpirationDate,
         InventoryItemVariantId, LocationId, SafetyStock, Remark)
    VALUES
        (N'食品', N'飲用水', N'600ml', 120, N'瓶', N'HasExpiry', '2027-12-31',
         @WaterVariantId, @Location1, 60, N'測試資料');
END

IF NOT EXISTS (SELECT 1 FROM SupplyItem WHERE LocationId=@Location1 AND InventoryItemVariantId=@DiaperLVariantId AND IsActive=1)
BEGIN
    INSERT INTO SupplyItem
        (Category, ItemName, Specification, Quantity, Unit, StockType,
         InventoryItemVariantId, LocationId, SafetyStock, Remark)
    VALUES
        (N'生活', N'尿布', N'L', 80, N'件', N'NoExpiry',
         @DiaperLVariantId, @Location1, 100, N'測試資料');
END

IF NOT EXISTS (SELECT 1 FROM SupplyItem WHERE LocationId=@Location3 AND InventoryItemVariantId=@DiaperXLVariantId AND IsActive=1)
BEGIN
    INSERT INTO SupplyItem
        (Category, ItemName, Specification, Quantity, Unit, StockType,
         InventoryItemVariantId, LocationId, SafetyStock, Remark)
    VALUES
        (N'生活', N'尿布', N'XL', 30, N'件', N'NoExpiry',
         @DiaperXLVariantId, @Location3, 10, N'測試資料');
END
GO

PRINT N'測試資料建立完成。';
GO

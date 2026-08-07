USE LocalSupplyDB;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InventoryTypeSetting')
BEGIN
    CREATE TABLE InventoryTypeSetting (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Category NVARCHAR(50) NOT NULL,
        ItemName NVARCHAR(100) NOT NULL,
        Specification NVARCHAR(200) NULL,
        Unit NVARCHAR(20) NULL,
        SafetyStock INT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NULL,
        CONSTRAINT CK_InventoryTypeSetting_SafetyStock CHECK (SafetyStock >= 0)
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('InventoryTypeSetting') AND name = 'Unit')
BEGIN
    ALTER TABLE InventoryTypeSetting ADD Unit NVARCHAR(20) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SupplyItem') AND name = 'InventoryTypeSettingId')
BEGIN
    ALTER TABLE SupplyItem ADD InventoryTypeSettingId INT NULL;
    ALTER TABLE SupplyItem ADD CONSTRAINT FK_SupplyItem_InventoryTypeSetting
        FOREIGN KEY (InventoryTypeSettingId) REFERENCES InventoryTypeSetting(Id);
    CREATE INDEX IX_SupplyItem_InventoryTypeSettingId ON SupplyItem(InventoryTypeSettingId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_InventoryTypeSetting_ActiveDefinition')
BEGIN
    CREATE UNIQUE INDEX UX_InventoryTypeSetting_ActiveDefinition
        ON InventoryTypeSetting(Category, ItemName, Specification)
        WHERE IsActive = 1;
END
GO

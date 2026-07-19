-- ================================================================
-- 地方物資管理平台 - 資料庫建立腳本
-- Database: LocalSupplyDB
-- Description: 建立資料庫與資料表結構
-- ================================================================

-- 建立資料庫
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'LocalSupplyDB')
BEGIN
    CREATE DATABASE LocalSupplyDB;
END
GO

USE LocalSupplyDB;
GO

-- ================================================================
-- 1. 據點資料表：SupplyLocation
-- ================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SupplyLocation')
BEGIN
    CREATE TABLE SupplyLocation (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        LocationName NVARCHAR(100) NOT NULL,
        Address NVARCHAR(200) NULL,
        Latitude DECIMAL(10,7) NULL,
        Longitude DECIMAL(10,7) NULL,
        ContactPerson NVARCHAR(50) NULL,
        Phone NVARCHAR(30) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NULL
    );
END
GO

-- ================================================================
-- 2. 物資資料表：SupplyItem
-- ================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SupplyItem')
BEGIN
    CREATE TABLE SupplyItem (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Category NVARCHAR(50) NOT NULL,
        ItemName NVARCHAR(100) NOT NULL,
        Specification NVARCHAR(200) NULL,
        Quantity INT NOT NULL DEFAULT 0,
        Unit NVARCHAR(20) NULL,
        StockType NVARCHAR(20) NOT NULL DEFAULT 'HasExpiry',
        ExpirationDate DATE NULL,
        ImagePath NVARCHAR(300) NULL,
        LocationId INT NOT NULL,
        SafetyStock INT NOT NULL DEFAULT 0,
        Remark NVARCHAR(300) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NULL,
        CONSTRAINT FK_SupplyItem_SupplyLocation FOREIGN KEY (LocationId)
            REFERENCES SupplyLocation(Id)
    );
END
GO

-- ================================================================
-- 3. 物資轉移紀錄表：SupplyTransferLog
-- ================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SupplyTransferLog')
BEGIN
    CREATE TABLE SupplyTransferLog (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        BatchId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        SupplyItemId INT NOT NULL,
        FromLocationId INT NOT NULL,
        ToLocationId INT NOT NULL,
        TransferQuantity INT NOT NULL,
        TransferTime DATETIME NOT NULL DEFAULT GETDATE(),
        Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
        ConfirmedBy NVARCHAR(50) NULL,
        ConfirmedAt DATETIME NULL,
        Operator NVARCHAR(50) NULL,
        Remark NVARCHAR(300) NULL,
        CONSTRAINT FK_TransferLog_SupplyItem FOREIGN KEY (SupplyItemId)
            REFERENCES SupplyItem(Id),
        CONSTRAINT FK_TransferLog_FromLocation FOREIGN KEY (FromLocationId)
            REFERENCES SupplyLocation(Id),
        CONSTRAINT FK_TransferLog_ToLocation FOREIGN KEY (ToLocationId)
            REFERENCES SupplyLocation(Id)
    );
END
GO

-- ================================================================
-- 4. 使用者帳號表：UserAccount
-- ================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserAccount')
BEGIN
    CREATE TABLE UserAccount (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserName NVARCHAR(50) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(300) NOT NULL,
        DisplayName NVARCHAR(50) NULL,
        RoleName NVARCHAR(30) NOT NULL DEFAULT 'User',
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NULL
    );
END
GO

-- ================================================================
-- 5. 物資出庫（發放）紀錄表：SupplyOutboundLog
-- ================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SupplyOutboundLog')
BEGIN
    CREATE TABLE SupplyOutboundLog (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SupplyItemId INT NOT NULL,
        LocationId INT NOT NULL,
        OutboundQuantity INT NOT NULL,
        RecipientName NVARCHAR(50) NOT NULL,
        RecipientContact NVARCHAR(50) NULL,
        Operator NVARCHAR(50) NULL,
        OutboundTime DATETIME NOT NULL DEFAULT GETDATE(),
        Remark NVARCHAR(300) NULL,
        CONSTRAINT FK_OutboundLog_SupplyItem FOREIGN KEY (SupplyItemId)
            REFERENCES SupplyItem(Id),
        CONSTRAINT FK_OutboundLog_Location FOREIGN KEY (LocationId)
            REFERENCES SupplyLocation(Id)
    );
END
GO
-- ================================================================
-- 6. 既有資料庫升級：補上規格/圖片/分類、轉移批次與確認欄位
-- ================================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SupplyItem') AND name = 'Specification')
BEGIN
    ALTER TABLE SupplyItem ADD Specification NVARCHAR(200) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SupplyItem') AND name = 'ImagePath')
BEGIN
    ALTER TABLE SupplyItem ADD ImagePath NVARCHAR(300) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SupplyItem') AND name = 'StockType')
BEGIN
    ALTER TABLE SupplyItem ADD StockType NVARCHAR(20) NOT NULL DEFAULT 'HasExpiry';
    EXEC('UPDATE SupplyItem SET StockType = ''NoExpiry'' WHERE ExpirationDate IS NULL');
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SupplyTransferLog') AND name = 'BatchId')
BEGIN
    ALTER TABLE SupplyTransferLog ADD BatchId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID();
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SupplyTransferLog') AND name = 'Status')
BEGIN
    ALTER TABLE SupplyTransferLog ADD Status NVARCHAR(20) NOT NULL DEFAULT 'Pending';
    -- 舊資料在新機制上線前已經是「送出即完成」，直接視為已確認，避免被誤重複加到目標據點庫存
    EXEC('UPDATE SupplyTransferLog SET Status = ''Confirmed'' WHERE Status = ''Pending''');
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SupplyTransferLog') AND name = 'ConfirmedBy')
BEGIN
    ALTER TABLE SupplyTransferLog ADD ConfirmedBy NVARCHAR(50) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SupplyTransferLog') AND name = 'ConfirmedAt')
BEGIN
    ALTER TABLE SupplyTransferLog ADD ConfirmedAt DATETIME NULL;
END
GO
-- ================================================================
-- 建立索引以提升查詢效能
-- ================================================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SupplyItem_LocationId')
BEGIN
    CREATE INDEX IX_SupplyItem_LocationId ON SupplyItem(LocationId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SupplyItem_Category')
BEGIN
    CREATE INDEX IX_SupplyItem_Category ON SupplyItem(Category);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SupplyTransferLog_FromLocationId')
BEGIN
    CREATE INDEX IX_SupplyTransferLog_FromLocationId ON SupplyTransferLog(FromLocationId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SupplyTransferLog_ToLocationId')
BEGIN
    CREATE INDEX IX_SupplyTransferLog_ToLocationId ON SupplyTransferLog(ToLocationId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SupplyOutboundLog_SupplyItemId')
BEGIN
    CREATE INDEX IX_SupplyOutboundLog_SupplyItemId ON SupplyOutboundLog(SupplyItemId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SupplyOutboundLog_LocationId')
BEGIN
    CREATE INDEX IX_SupplyOutboundLog_LocationId ON SupplyOutboundLog(LocationId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SupplyItem_StockType')
BEGIN
    CREATE INDEX IX_SupplyItem_StockType ON SupplyItem(StockType);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SupplyTransferLog_BatchId')
BEGIN
    CREATE INDEX IX_SupplyTransferLog_BatchId ON SupplyTransferLog(BatchId);
END
GO

PRINT '資料庫與資料表建立完成！';
GO

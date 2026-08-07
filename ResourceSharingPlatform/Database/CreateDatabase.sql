-- 地方物資管理平台 - 完整資料庫建立腳本
-- Version: 2026-08-07
-- Database: LocalSupplyDB

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF DB_ID(N'LocalSupplyDB') IS NULL
BEGIN
    CREATE DATABASE LocalSupplyDB;
END
GO

USE LocalSupplyDB;
GO

IF OBJECT_ID(N'SupplyLocation', N'U') IS NULL
BEGIN
    CREATE TABLE SupplyLocation (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SupplyLocation PRIMARY KEY,
        LocationName NVARCHAR(100) NOT NULL,
        Address NVARCHAR(200) NULL,
        Latitude DECIMAL(10,7) NULL,
        Longitude DECIMAL(10,7) NULL,
        ContactPerson NVARCHAR(50) NULL,
        Phone NVARCHAR(30) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_SupplyLocation_IsActive DEFAULT 1,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_SupplyLocation_CreatedAt DEFAULT GETDATE(),
        UpdatedAt DATETIME NULL
    );
END
GO

IF OBJECT_ID(N'InventoryItemDefinition', N'U') IS NULL
BEGIN
    CREATE TABLE InventoryItemDefinition (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventoryItemDefinition PRIMARY KEY,
        Category NVARCHAR(50) NOT NULL,
        ItemName NVARCHAR(100) NOT NULL,
        Unit NVARCHAR(20) NOT NULL,
        GlobalSafetyStock INT NOT NULL CONSTRAINT DF_InventoryItemDefinition_GlobalSafetyStock DEFAULT 0,
        IsActive BIT NOT NULL CONSTRAINT DF_InventoryItemDefinition_IsActive DEFAULT 1,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_InventoryItemDefinition_CreatedAt DEFAULT GETDATE(),
        UpdatedAt DATETIME NULL,
        CONSTRAINT CK_InventoryItemDefinition_GlobalSafetyStock CHECK (GlobalSafetyStock >= 0)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'InventoryItemDefinition') AND name = N'UX_InventoryItemDefinition_ActiveName')
BEGIN
    CREATE UNIQUE INDEX UX_InventoryItemDefinition_ActiveName
        ON InventoryItemDefinition(Category, ItemName)
        WHERE IsActive = 1;
END
GO

IF OBJECT_ID(N'InventoryItemVariant', N'U') IS NULL
BEGIN
    CREATE TABLE InventoryItemVariant (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventoryItemVariant PRIMARY KEY,
        InventoryItemDefinitionId INT NOT NULL,
        Specification NVARCHAR(200) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_InventoryItemVariant_IsActive DEFAULT 1,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_InventoryItemVariant_CreatedAt DEFAULT GETDATE(),
        UpdatedAt DATETIME NULL,
        CONSTRAINT FK_InventoryItemVariant_Definition FOREIGN KEY (InventoryItemDefinitionId)
            REFERENCES InventoryItemDefinition(Id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'InventoryItemVariant') AND name = N'IX_InventoryItemVariant_DefinitionId')
    CREATE INDEX IX_InventoryItemVariant_DefinitionId ON InventoryItemVariant(InventoryItemDefinitionId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'InventoryItemVariant') AND name = N'UX_InventoryItemVariant_ActiveSpecification')
BEGIN
    CREATE UNIQUE INDEX UX_InventoryItemVariant_ActiveSpecification
        ON InventoryItemVariant(InventoryItemDefinitionId, Specification)
        WHERE IsActive = 1;
END
GO

-- 舊版相容表：新功能不再寫入，保留供既有資料遷移與追溯。
IF OBJECT_ID(N'dbo.InventoryTypeSetting', N'U') IS NULL
BEGIN
    EXEC(N'
        CREATE TABLE dbo.InventoryTypeSetting (
            Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventoryTypeSetting PRIMARY KEY,
            Category NVARCHAR(50) NOT NULL,
            ItemName NVARCHAR(100) NOT NULL,
            Specification NVARCHAR(200) NULL,
            Unit NVARCHAR(20) NULL,
            SafetyStock INT NOT NULL CONSTRAINT DF_InventoryTypeSetting_SafetyStock DEFAULT 0,
            IsActive BIT NOT NULL CONSTRAINT DF_InventoryTypeSetting_IsActive DEFAULT 1,
            CreatedAt DATETIME NOT NULL CONSTRAINT DF_InventoryTypeSetting_CreatedAt DEFAULT GETDATE(),
            UpdatedAt DATETIME NULL,
            CONSTRAINT CK_InventoryTypeSetting_SafetyStock CHECK (SafetyStock >= 0)
        );
    ');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'InventoryTypeSetting') AND name = N'UX_InventoryTypeSetting_ActiveDefinition')
BEGIN
    CREATE UNIQUE INDEX UX_InventoryTypeSetting_ActiveDefinition
        ON InventoryTypeSetting(Category, ItemName, Specification)
        WHERE IsActive = 1;
END
GO

IF OBJECT_ID(N'SupplyItem', N'U') IS NULL
BEGIN
    CREATE TABLE SupplyItem (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SupplyItem PRIMARY KEY,
        Category NVARCHAR(50) NOT NULL,
        ItemName NVARCHAR(100) NOT NULL,
        Specification NVARCHAR(200) NULL,
        Quantity INT NOT NULL CONSTRAINT DF_SupplyItem_Quantity DEFAULT 0,
        Unit NVARCHAR(20) NULL,
        StockType NVARCHAR(20) NOT NULL CONSTRAINT DF_SupplyItem_StockType DEFAULT N'HasExpiry',
        ExpirationDate DATE NULL,
        ImagePath NVARCHAR(300) NULL,
        InventoryTypeSettingId INT NULL,
        InventoryItemVariantId INT NULL,
        LocationId INT NOT NULL,
        SafetyStock INT NOT NULL CONSTRAINT DF_SupplyItem_SafetyStock DEFAULT 0,
        Remark NVARCHAR(300) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_SupplyItem_IsActive DEFAULT 1,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_SupplyItem_CreatedAt DEFAULT GETDATE(),
        UpdatedAt DATETIME NULL,
        CONSTRAINT CK_SupplyItem_Quantity CHECK (Quantity >= 0),
        CONSTRAINT CK_SupplyItem_SafetyStock CHECK (SafetyStock >= 0),
        CONSTRAINT CK_SupplyItem_StockType CHECK (StockType IN (N'NoExpiry', N'HasExpiry', N'Frozen')),
        CONSTRAINT FK_SupplyItem_Location FOREIGN KEY (LocationId) REFERENCES SupplyLocation(Id),
        CONSTRAINT FK_SupplyItem_InventoryTypeSetting FOREIGN KEY (InventoryTypeSettingId) REFERENCES InventoryTypeSetting(Id),
        CONSTRAINT FK_SupplyItem_InventoryItemVariant FOREIGN KEY (InventoryItemVariantId) REFERENCES InventoryItemVariant(Id)
    );
END
GO

IF OBJECT_ID(N'LocationInventorySafetyStock', N'U') IS NULL
BEGIN
    CREATE TABLE LocationInventorySafetyStock (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LocationInventorySafetyStock PRIMARY KEY,
        LocationId INT NOT NULL,
        InventoryItemDefinitionId INT NOT NULL,
        SafetyStock INT NOT NULL CONSTRAINT DF_LocationInventorySafetyStock_SafetyStock DEFAULT 0,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_LocationInventorySafetyStock_CreatedAt DEFAULT GETDATE(),
        UpdatedAt DATETIME NULL,
        CONSTRAINT CK_LocationInventorySafetyStock_SafetyStock CHECK (SafetyStock >= 0),
        CONSTRAINT FK_LocationInventorySafetyStock_Location FOREIGN KEY (LocationId) REFERENCES SupplyLocation(Id),
        CONSTRAINT FK_LocationInventorySafetyStock_Definition FOREIGN KEY (InventoryItemDefinitionId) REFERENCES InventoryItemDefinition(Id),
        CONSTRAINT UX_LocationInventorySafetyStock UNIQUE (LocationId, InventoryItemDefinitionId)
    );
END
GO

IF OBJECT_ID(N'UserAccount', N'U') IS NULL
BEGIN
    CREATE TABLE UserAccount (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserAccount PRIMARY KEY,
        UserName NVARCHAR(50) NOT NULL,
        PasswordHash NVARCHAR(300) NOT NULL,
        DisplayName NVARCHAR(50) NULL,
        RoleName NVARCHAR(30) NOT NULL CONSTRAINT DF_UserAccount_RoleName DEFAULT N'SocialWorker',
        LocationId INT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_UserAccount_IsActive DEFAULT 1,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_UserAccount_CreatedAt DEFAULT GETDATE(),
        UpdatedAt DATETIME NULL,
        CONSTRAINT UQ_UserAccount_UserName UNIQUE (UserName),
        CONSTRAINT CK_UserAccount_RoleName CHECK (RoleName IN (N'Admin', N'Cadre', N'SocialWorker')),
        CONSTRAINT FK_UserAccount_Location FOREIGN KEY (LocationId) REFERENCES SupplyLocation(Id)
    );
END
GO

IF OBJECT_ID(N'SupplyTransferLog', N'U') IS NULL
BEGIN
    CREATE TABLE SupplyTransferLog (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SupplyTransferLog PRIMARY KEY,
        BatchId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_SupplyTransferLog_BatchId DEFAULT NEWID(),
        SupplyItemId INT NOT NULL,
        FromLocationId INT NOT NULL,
        ToLocationId INT NOT NULL,
        TransferQuantity INT NOT NULL,
        TransferTime DATETIME NOT NULL CONSTRAINT DF_SupplyTransferLog_TransferTime DEFAULT GETDATE(),
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_SupplyTransferLog_Status DEFAULT N'Pending',
        ConfirmedBy NVARCHAR(50) NULL,
        ConfirmedAt DATETIME NULL,
        Operator NVARCHAR(50) NULL,
        Remark NVARCHAR(300) NULL,
        CONSTRAINT CK_SupplyTransferLog_Quantity CHECK (TransferQuantity > 0),
        CONSTRAINT CK_SupplyTransferLog_Locations CHECK (FromLocationId <> ToLocationId),
        CONSTRAINT CK_SupplyTransferLog_Status CHECK (Status IN (N'Pending', N'Confirmed', N'Cancelled')),
        CONSTRAINT FK_SupplyTransferLog_Item FOREIGN KEY (SupplyItemId) REFERENCES SupplyItem(Id),
        CONSTRAINT FK_SupplyTransferLog_FromLocation FOREIGN KEY (FromLocationId) REFERENCES SupplyLocation(Id),
        CONSTRAINT FK_SupplyTransferLog_ToLocation FOREIGN KEY (ToLocationId) REFERENCES SupplyLocation(Id)
    );
END
GO

IF OBJECT_ID(N'SupplyOutboundLog', N'U') IS NULL
BEGIN
    CREATE TABLE SupplyOutboundLog (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SupplyOutboundLog PRIMARY KEY,
        SupplyItemId INT NOT NULL,
        LocationId INT NOT NULL,
        OutboundQuantity INT NOT NULL,
        RecipientName NVARCHAR(50) NOT NULL,
        RecipientContact NVARCHAR(50) NULL,
        Operator NVARCHAR(50) NULL,
        OutboundTime DATETIME NOT NULL CONSTRAINT DF_SupplyOutboundLog_OutboundTime DEFAULT GETDATE(),
        Remark NVARCHAR(300) NULL,
        CONSTRAINT CK_SupplyOutboundLog_Quantity CHECK (OutboundQuantity > 0),
        CONSTRAINT FK_SupplyOutboundLog_Item FOREIGN KEY (SupplyItemId) REFERENCES SupplyItem(Id),
        CONSTRAINT FK_SupplyOutboundLog_Location FOREIGN KEY (LocationId) REFERENCES SupplyLocation(Id)
    );
END
GO

IF OBJECT_ID(N'SupplyDonationLog', N'U') IS NULL
BEGIN
    CREATE TABLE SupplyDonationLog (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SupplyDonationLog PRIMARY KEY,
        SupplyItemId INT NOT NULL,
        LocationId INT NOT NULL,
        DonationQuantity INT NOT NULL,
        DonorName NVARCHAR(50) NOT NULL,
        DonorContact NVARCHAR(50) NULL,
        Operator NVARCHAR(50) NULL,
        DonationTime DATETIME NOT NULL CONSTRAINT DF_SupplyDonationLog_DonationTime DEFAULT GETDATE(),
        Remark NVARCHAR(300) NULL,
        CONSTRAINT CK_SupplyDonationLog_Quantity CHECK (DonationQuantity > 0),
        CONSTRAINT FK_SupplyDonationLog_Item FOREIGN KEY (SupplyItemId) REFERENCES SupplyItem(Id),
        CONSTRAINT FK_SupplyDonationLog_Location FOREIGN KEY (LocationId) REFERENCES SupplyLocation(Id)
    );
END
GO

IF OBJECT_ID(N'SupplyDisposalLog', N'U') IS NULL
BEGIN
    CREATE TABLE SupplyDisposalLog (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SupplyDisposalLog PRIMARY KEY,
        SupplyItemId INT NOT NULL,
        LocationId INT NOT NULL,
        DisposalQuantity INT NOT NULL,
        Reason NVARCHAR(20) NOT NULL CONSTRAINT DF_SupplyDisposalLog_Reason DEFAULT N'Other',
        Operator NVARCHAR(50) NULL,
        DisposalTime DATETIME NOT NULL CONSTRAINT DF_SupplyDisposalLog_DisposalTime DEFAULT GETDATE(),
        Remark NVARCHAR(300) NULL,
        CONSTRAINT CK_SupplyDisposalLog_Quantity CHECK (DisposalQuantity > 0),
        CONSTRAINT CK_SupplyDisposalLog_Reason CHECK (Reason IN (N'Expired', N'Damaged', N'Lost', N'Other')),
        CONSTRAINT FK_SupplyDisposalLog_Item FOREIGN KEY (SupplyItemId) REFERENCES SupplyItem(Id),
        CONSTRAINT FK_SupplyDisposalLog_Location FOREIGN KEY (LocationId) REFERENCES SupplyLocation(Id)
    );
END
GO

IF OBJECT_ID(N'LineNotificationSettings', N'U') IS NULL
BEGIN
    CREATE TABLE LineNotificationSettings (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LineNotificationSettings PRIMARY KEY,
        IsEnabled BIT NOT NULL CONSTRAINT DF_LineNotificationSettings_IsEnabled DEFAULT 0,
        ChannelAccessToken NVARCHAR(300) NULL,
        ChannelSecret NVARCHAR(300) NULL,
        NotifyLowStock BIT NOT NULL CONSTRAINT DF_LineNotificationSettings_NotifyLowStock DEFAULT 1,
        NotifyExpiringSoon BIT NOT NULL CONSTRAINT DF_LineNotificationSettings_NotifyExpiringSoon DEFAULT 1,
        NotifyExpired BIT NOT NULL CONSTRAINT DF_LineNotificationSettings_NotifyExpired DEFAULT 1,
        UpdatedAt DATETIME NULL,
        UpdatedBy NVARCHAR(50) NULL
    );
END
GO

IF OBJECT_ID(N'AIStockInSettings', N'U') IS NULL
BEGIN
    CREATE TABLE AIStockInSettings (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AIStockInSettings PRIMARY KEY,
        IsEnabled BIT NOT NULL CONSTRAINT DF_AIStockInSettings_IsEnabled DEFAULT 0,
        ApiEndpoint NVARCHAR(300) NULL,
        ApiKey NVARCHAR(300) NULL,
        SupportsImageInput BIT NOT NULL CONSTRAINT DF_AIStockInSettings_SupportsImage DEFAULT 1,
        SupportsTextInput BIT NOT NULL CONSTRAINT DF_AIStockInSettings_SupportsText DEFAULT 1,
        UpdatedAt DATETIME NULL,
        UpdatedBy NVARCHAR(50) NULL
    );
END
GO

IF OBJECT_ID(N'AIStockInLog', N'U') IS NULL
BEGIN
    CREATE TABLE AIStockInLog (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AIStockInLog PRIMARY KEY,
        LocationId INT NOT NULL,
        InputType NVARCHAR(20) NOT NULL CONSTRAINT DF_AIStockInLog_InputType DEFAULT N'Image',
        InputText NVARCHAR(500) NULL,
        InputImagePath NVARCHAR(300) NULL,
        SuggestedCategory NVARCHAR(50) NULL,
        SuggestedItemName NVARCHAR(100) NULL,
        SuggestedSpecification NVARCHAR(200) NULL,
        SuggestedQuantity INT NULL,
        SuggestedUnit NVARCHAR(20) NULL,
        SuggestedStockType NVARCHAR(20) NULL,
        SuggestedExpirationDate DATE NULL,
        SuggestedSafetyStock INT NULL,
        SuggestedRemark NVARCHAR(300) NULL,
        Confidence DECIMAL(5,4) NULL,
        RawResponse NVARCHAR(MAX) NULL,
        IsConfirmed BIT NOT NULL CONSTRAINT DF_AIStockInLog_IsConfirmed DEFAULT 0,
        ConfirmedSupplyItemId INT NULL,
        Operator NVARCHAR(50) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_AIStockInLog_CreatedAt DEFAULT GETDATE(),
        ConfirmedAt DATETIME NULL,
        CONSTRAINT CK_AIStockInLog_InputType CHECK (InputType IN (N'Image', N'Text')),
        CONSTRAINT CK_AIStockInLog_Confidence CHECK (Confidence IS NULL OR (Confidence >= 0 AND Confidence <= 1)),
        CONSTRAINT FK_AIStockInLog_Location FOREIGN KEY (LocationId) REFERENCES SupplyLocation(Id),
        CONSTRAINT FK_AIStockInLog_Item FOREIGN KEY (ConfirmedSupplyItemId) REFERENCES SupplyItem(Id)
    );
END
GO

-- 查詢與外鍵索引
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'SupplyItem') AND name = N'IX_SupplyItem_LocationId')
    CREATE INDEX IX_SupplyItem_LocationId ON SupplyItem(LocationId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'SupplyItem') AND name = N'IX_SupplyItem_Category')
    CREATE INDEX IX_SupplyItem_Category ON SupplyItem(Category);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'SupplyItem') AND name = N'IX_SupplyItem_StockType')
    CREATE INDEX IX_SupplyItem_StockType ON SupplyItem(StockType);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'SupplyItem') AND name = N'IX_SupplyItem_InventoryItemVariantId')
    CREATE INDEX IX_SupplyItem_InventoryItemVariantId ON SupplyItem(InventoryItemVariantId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'SupplyItem') AND name = N'IX_SupplyItem_InventoryTypeSettingId')
    CREATE INDEX IX_SupplyItem_InventoryTypeSettingId ON SupplyItem(InventoryTypeSettingId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'UserAccount') AND name = N'IX_UserAccount_LocationId')
    CREATE INDEX IX_UserAccount_LocationId ON UserAccount(LocationId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'SupplyTransferLog') AND name = N'IX_SupplyTransferLog_BatchId')
    CREATE INDEX IX_SupplyTransferLog_BatchId ON SupplyTransferLog(BatchId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'SupplyTransferLog') AND name = N'IX_SupplyTransferLog_FromLocationId')
    CREATE INDEX IX_SupplyTransferLog_FromLocationId ON SupplyTransferLog(FromLocationId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'SupplyTransferLog') AND name = N'IX_SupplyTransferLog_ToLocationId')
    CREATE INDEX IX_SupplyTransferLog_ToLocationId ON SupplyTransferLog(ToLocationId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'SupplyOutboundLog') AND name = N'IX_SupplyOutboundLog_SupplyItemId')
    CREATE INDEX IX_SupplyOutboundLog_SupplyItemId ON SupplyOutboundLog(SupplyItemId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'SupplyOutboundLog') AND name = N'IX_SupplyOutboundLog_LocationId')
    CREATE INDEX IX_SupplyOutboundLog_LocationId ON SupplyOutboundLog(LocationId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'SupplyDonationLog') AND name = N'IX_SupplyDonationLog_SupplyItemId')
    CREATE INDEX IX_SupplyDonationLog_SupplyItemId ON SupplyDonationLog(SupplyItemId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'SupplyDonationLog') AND name = N'IX_SupplyDonationLog_LocationId')
    CREATE INDEX IX_SupplyDonationLog_LocationId ON SupplyDonationLog(LocationId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'SupplyDisposalLog') AND name = N'IX_SupplyDisposalLog_SupplyItemId')
    CREATE INDEX IX_SupplyDisposalLog_SupplyItemId ON SupplyDisposalLog(SupplyItemId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'SupplyDisposalLog') AND name = N'IX_SupplyDisposalLog_LocationId')
    CREATE INDEX IX_SupplyDisposalLog_LocationId ON SupplyDisposalLog(LocationId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'AIStockInLog') AND name = N'IX_AIStockInLog_LocationId')
    CREATE INDEX IX_AIStockInLog_LocationId ON AIStockInLog(LocationId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'AIStockInLog') AND name = N'IX_AIStockInLog_ConfirmedSupplyItemId')
    CREATE INDEX IX_AIStockInLog_ConfirmedSupplyItemId ON AIStockInLog(ConfirmedSupplyItemId);
GO

PRINT N'LocalSupplyDB 最新版資料庫結構建立完成。';
GO

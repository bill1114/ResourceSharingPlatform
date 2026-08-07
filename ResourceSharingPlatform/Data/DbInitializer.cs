using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ResourceSharingPlatform.Models;

namespace ResourceSharingPlatform.Data
{
    public static class DbInitializer
    {
        public static async Task EnsureInventoryTypeSettingTableAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Each ExecuteSqlRawAsync call is its own batch (there is no "GO" separator over
            // this ADO.NET connection - "GO" is a sqlcmd/SSMS-only client directive, not real
            // T-SQL). SQL Server compiles a whole batch's column references up front, so an
            // ALTER TABLE ADD COLUMN and a later statement that references that column must be
            // split into separate calls, or the later statement fails with "invalid column
            // name" even though the ALTER already ran earlier in the same original string.
            await context.Database.ExecuteSqlRawAsync("""
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
                END;

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_InventoryTypeSetting_ActiveDefinition')
                BEGIN
                    CREATE UNIQUE INDEX UX_InventoryTypeSetting_ActiveDefinition
                        ON InventoryTypeSetting(Category, ItemName, Specification)
                        WHERE IsActive = 1;
                END;
                """);

            await context.Database.ExecuteSqlRawAsync("""
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SupplyItem') AND name = 'InventoryTypeSettingId')
                BEGIN
                    ALTER TABLE SupplyItem ADD InventoryTypeSettingId INT NULL;
                    ALTER TABLE SupplyItem ADD CONSTRAINT FK_SupplyItem_InventoryTypeSetting
                        FOREIGN KEY (InventoryTypeSettingId) REFERENCES InventoryTypeSetting(Id);
                    CREATE INDEX IX_SupplyItem_InventoryTypeSettingId ON SupplyItem(InventoryTypeSettingId);
                END;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('InventoryTypeSetting') AND name = 'Unit')
                BEGIN
                    ALTER TABLE InventoryTypeSetting ADD Unit NVARCHAR(20) NULL;
                END;
                """);

            await context.Database.ExecuteSqlRawAsync("""
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InventoryItemDefinition')
                BEGIN
                    CREATE TABLE InventoryItemDefinition (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Category NVARCHAR(50) NOT NULL,
                        ItemName NVARCHAR(100) NOT NULL,
                        Unit NVARCHAR(20) NOT NULL,
                        GlobalSafetyStock INT NOT NULL DEFAULT 0,
                        IsActive BIT NOT NULL DEFAULT 1,
                        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
                        UpdatedAt DATETIME NULL,
                        CONSTRAINT CK_InventoryItemDefinition_GlobalSafetyStock CHECK (GlobalSafetyStock >= 0)
                    );
                    CREATE UNIQUE INDEX UX_InventoryItemDefinition_ActiveName
                        ON InventoryItemDefinition(Category, ItemName) WHERE IsActive = 1;
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InventoryItemVariant')
                BEGIN
                    CREATE TABLE InventoryItemVariant (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        InventoryItemDefinitionId INT NOT NULL,
                        Specification NVARCHAR(200) NULL,
                        IsActive BIT NOT NULL DEFAULT 1,
                        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
                        UpdatedAt DATETIME NULL,
                        CONSTRAINT FK_InventoryItemVariant_Definition FOREIGN KEY (InventoryItemDefinitionId)
                            REFERENCES InventoryItemDefinition(Id)
                    );
                    CREATE INDEX IX_InventoryItemVariant_DefinitionId
                        ON InventoryItemVariant(InventoryItemDefinitionId);
                    CREATE UNIQUE INDEX UX_InventoryItemVariant_ActiveSpecification
                        ON InventoryItemVariant(InventoryItemDefinitionId, Specification) WHERE IsActive = 1;
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LocationInventorySafetyStock')
                BEGIN
                    CREATE TABLE LocationInventorySafetyStock (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        LocationId INT NOT NULL,
                        InventoryItemDefinitionId INT NOT NULL,
                        SafetyStock INT NOT NULL DEFAULT 0,
                        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
                        UpdatedAt DATETIME NULL,
                        CONSTRAINT CK_LocationInventorySafetyStock_SafetyStock CHECK (SafetyStock >= 0),
                        CONSTRAINT FK_LocationInventorySafetyStock_Location FOREIGN KEY (LocationId)
                            REFERENCES SupplyLocation(Id),
                        CONSTRAINT FK_LocationInventorySafetyStock_Definition FOREIGN KEY (InventoryItemDefinitionId)
                            REFERENCES InventoryItemDefinition(Id)
                    );
                    CREATE UNIQUE INDEX UX_LocationInventorySafetyStock_LocationDefinition
                        ON LocationInventorySafetyStock(LocationId, InventoryItemDefinitionId);
                END;
                """);

            await context.Database.ExecuteSqlRawAsync("""
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SupplyItem') AND name = 'InventoryItemVariantId')
                BEGIN
                    ALTER TABLE SupplyItem ADD InventoryItemVariantId INT NULL;
                    ALTER TABLE SupplyItem ADD CONSTRAINT FK_SupplyItem_InventoryItemVariant
                        FOREIGN KEY (InventoryItemVariantId) REFERENCES InventoryItemVariant(Id);
                    CREATE INDEX IX_SupplyItem_InventoryItemVariantId ON SupplyItem(InventoryItemVariantId);
                END;
                """);

            await context.Database.ExecuteSqlRawAsync("""
                IF NOT EXISTS (SELECT 1 FROM InventoryItemDefinition)
                BEGIN
                INSERT INTO InventoryItemDefinition
                    (Category, ItemName, Unit, GlobalSafetyStock, IsActive, CreatedAt)
                SELECT s.Category, s.ItemName,
                       COALESCE(NULLIF(MAX(s.Unit), ''), N'個'),
                       MAX(s.SafetyStock), MAX(CAST(s.IsActive AS INT)), MIN(s.CreatedAt)
                FROM InventoryTypeSetting s
                WHERE NOT EXISTS (
                    SELECT 1 FROM InventoryItemDefinition d
                    WHERE d.Category = s.Category AND d.ItemName = s.ItemName AND d.IsActive = 1
                )
                GROUP BY s.Category, s.ItemName;

                INSERT INTO InventoryItemVariant
                    (InventoryItemDefinitionId, Specification, IsActive, CreatedAt)
                SELECT d.Id, s.Specification, MAX(CAST(s.IsActive AS INT)), MIN(s.CreatedAt)
                FROM InventoryTypeSetting s
                INNER JOIN InventoryItemDefinition d
                    ON d.Category = s.Category AND d.ItemName = s.ItemName AND d.IsActive = 1
                WHERE NOT EXISTS (
                    SELECT 1 FROM InventoryItemVariant v
                    WHERE v.InventoryItemDefinitionId = d.Id
                      AND (v.Specification = s.Specification OR (v.Specification IS NULL AND s.Specification IS NULL))
                      AND v.IsActive = 1
                )
                GROUP BY d.Id, s.Specification;

                UPDATE si
                SET InventoryItemVariantId = v.Id
                FROM SupplyItem si
                INNER JOIN InventoryTypeSetting oldSetting ON oldSetting.Id = si.InventoryTypeSettingId
                INNER JOIN InventoryItemDefinition d
                    ON d.Category = oldSetting.Category AND d.ItemName = oldSetting.ItemName AND d.IsActive = 1
                INNER JOIN InventoryItemVariant v
                    ON v.InventoryItemDefinitionId = d.Id
                    AND (v.Specification = oldSetting.Specification OR (v.Specification IS NULL AND oldSetting.Specification IS NULL))
                    AND v.IsActive = 1
                WHERE si.InventoryItemVariantId IS NULL;

                UPDATE si
                SET InventoryItemVariantId = v.Id
                FROM SupplyItem si
                INNER JOIN InventoryItemDefinition d
                    ON d.Category = si.Category AND d.ItemName = si.ItemName AND d.IsActive = 1
                INNER JOIN InventoryItemVariant v
                    ON v.InventoryItemDefinitionId = d.Id
                    AND (v.Specification = si.Specification OR (v.Specification IS NULL AND si.Specification IS NULL))
                    AND v.IsActive = 1
                WHERE si.InventoryItemVariantId IS NULL;

                INSERT INTO LocationInventorySafetyStock
                    (LocationId, InventoryItemDefinitionId, SafetyStock, CreatedAt)
                SELECT si.LocationId, d.Id, MAX(si.SafetyStock), GETDATE()
                FROM SupplyItem si
                INNER JOIN InventoryItemDefinition d
                    ON d.Category = si.Category AND d.ItemName = si.ItemName AND d.IsActive = 1
                WHERE si.IsActive = 1
                  AND NOT EXISTS (
                      SELECT 1 FROM LocationInventorySafetyStock ls
                      WHERE ls.LocationId = si.LocationId AND ls.InventoryItemDefinitionId = d.Id
                  )
                GROUP BY si.LocationId, d.Id;
                END;
                """);
        }

        // Deployments that never populated the legacy InventoryTypeSetting table (e.g. this one -
        // the InventoryTypeSetting/Definition feature was developed on another machine against a
        // different database) get no InventoryItemDefinition rows out of
        // EnsureInventoryTypeSettingTableAsync's InventoryTypeSetting-sourced backfill, which would
        // leave SupplyItemController.Create permanently blocked (it now requires a valid
        // InventoryItemVariantId) even though real SupplyItem data already exists. This backfills
        // definitions/variants/location safety stocks directly from live SupplyItem rows instead.
        // Idempotent: no-op once InventoryItemDefinition has any rows.
        public static async Task BackfillInventoryDefinitionsFromSupplyItemsAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await context.Database.ExecuteSqlRawAsync("""
                IF NOT EXISTS (SELECT 1 FROM InventoryItemDefinition)
                BEGIN
                    INSERT INTO InventoryItemDefinition
                        (Category, ItemName, Unit, GlobalSafetyStock, IsActive, CreatedAt)
                    SELECT si.Category, si.ItemName,
                           COALESCE(NULLIF(MAX(si.Unit), ''), N'個'),
                           0, 1, MIN(si.CreatedAt)
                    FROM SupplyItem si
                    WHERE si.IsActive = 1
                    GROUP BY si.Category, si.ItemName;

                    INSERT INTO InventoryItemVariant
                        (InventoryItemDefinitionId, Specification, IsActive, CreatedAt)
                    SELECT d.Id, si.Specification, 1, MIN(si.CreatedAt)
                    FROM SupplyItem si
                    INNER JOIN InventoryItemDefinition d
                        ON d.Category = si.Category AND d.ItemName = si.ItemName AND d.IsActive = 1
                    WHERE si.IsActive = 1
                    GROUP BY d.Id, si.Specification;

                    UPDATE si
                    SET InventoryItemVariantId = v.Id
                    FROM SupplyItem si
                    INNER JOIN InventoryItemDefinition d
                        ON d.Category = si.Category AND d.ItemName = si.ItemName AND d.IsActive = 1
                    INNER JOIN InventoryItemVariant v
                        ON v.InventoryItemDefinitionId = d.Id
                        AND (v.Specification = si.Specification OR (v.Specification IS NULL AND si.Specification IS NULL))
                        AND v.IsActive = 1
                    WHERE si.IsActive = 1 AND si.InventoryItemVariantId IS NULL;

                    INSERT INTO LocationInventorySafetyStock
                        (LocationId, InventoryItemDefinitionId, SafetyStock, CreatedAt)
                    SELECT si.LocationId, d.Id, MAX(si.SafetyStock), GETDATE()
                    FROM SupplyItem si
                    INNER JOIN InventoryItemDefinition d
                        ON d.Category = si.Category AND d.ItemName = si.ItemName AND d.IsActive = 1
                    WHERE si.IsActive = 1
                      AND NOT EXISTS (
                          SELECT 1 FROM LocationInventorySafetyStock ls
                          WHERE ls.LocationId = si.LocationId AND ls.InventoryItemDefinitionId = d.Id
                      )
                    GROUP BY si.LocationId, d.Id;
                END;
                """);
        }

        public static async Task SeedAdminAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<UserAccount>>();

            if (await context.UserAccounts.AnyAsync())
            {
                return;
            }

            var admin = new UserAccount
            {
                UserName = "admin",
                DisplayName = "系統管理員",
                RoleName = Roles.Admin,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            admin.PasswordHash = hasher.HashPassword(admin, "admin");

            context.UserAccounts.Add(admin);
            await context.SaveChangesAsync();
        }

        // Merges duplicate active SupplyItem rows (same item, same location) created before
        // SupplyItemController.Create started merging on insert. Idempotent - a no-op once
        // there are no duplicates left.
        public static async Task MergeDuplicateItemsAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var activeItems = await context.SupplyItems
                .Where(x => x.IsActive)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();

            var duplicateGroups = activeItems
                .GroupBy(x => (x.LocationId, x.ItemName, x.Category, x.Specification, x.StockType, x.ExpirationDate))
                .Where(g => g.Count() > 1);

            var hasChanges = false;

            foreach (var group in duplicateGroups)
            {
                var ordered = group.OrderBy(x => x.CreatedAt).ToList();
                var keeper = ordered[0];

                foreach (var duplicate in ordered.Skip(1))
                {
                    keeper.Quantity += duplicate.Quantity;
                    if (string.IsNullOrEmpty(keeper.ImagePath) && !string.IsNullOrEmpty(duplicate.ImagePath))
                    {
                        keeper.ImagePath = duplicate.ImagePath;
                    }

                    duplicate.IsActive = false;
                    duplicate.UpdatedAt = DateTime.Now;
                }

                keeper.UpdatedAt = DateTime.Now;
                hasChanges = true;
            }

            if (hasChanges)
            {
                await context.SaveChangesAsync();
            }
        }

        // Ensures a single LineNotificationSettings row exists (disabled by default,
        // no credentials) so the settings page always has something to edit.
        public static async Task EnsureLineSettingsAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (await context.LineNotificationSettings.AnyAsync())
            {
                return;
            }

            context.LineNotificationSettings.Add(new LineNotificationSettings
            {
                IsEnabled = false,
                NotifyLowStock = true,
                NotifyExpiringSoon = true,
                NotifyExpired = true
            });

            await context.SaveChangesAsync();
        }

        // Ensures a single AIStockInSettings row exists (disabled, no endpoint/key)
        // so the settings page has something to edit once the AI model is wired up.
        public static async Task EnsureAIStockInSettingsAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (await context.AIStockInSettings.AnyAsync())
            {
                return;
            }

            context.AIStockInSettings.Add(new AIStockInSettings
            {
                IsEnabled = false,
                SupportsImageInput = true,
                SupportsTextInput = true
            });

            await context.SaveChangesAsync();
        }
    }
}

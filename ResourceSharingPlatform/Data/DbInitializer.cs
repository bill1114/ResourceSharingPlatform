using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ResourceSharingPlatform.Models;

namespace ResourceSharingPlatform.Data
{
    public static class DbInitializer
    {
        // InventoryTypeSetting was a transitional table from before InventoryItemDefinition/
        // InventoryItemVariant existed, and it was removed once confirmed dead (0 rows, no
        // Controller/Service/View reads or writes it - InventoryItemDefinition is backfilled
        // directly from live SupplyItem rows by BackfillInventoryDefinitionsFromSupplyItemsAsync
        // instead). The DROP block below is idempotent cleanup for any existing installation
        // that still has the old table/column; on a fresh database it is a no-op.
        public static async Task EnsureInventoryCatalogTablesAsync(IServiceProvider services)
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
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SupplyItem') AND name = 'InventoryTypeSettingId')
                BEGIN
                    DECLARE @fk NVARCHAR(200) = (
                        SELECT name FROM sys.foreign_keys
                        WHERE parent_object_id = OBJECT_ID('SupplyItem')
                          AND referenced_object_id = OBJECT_ID('InventoryTypeSetting')
                    );
                    IF @fk IS NOT NULL
                        EXEC('ALTER TABLE SupplyItem DROP CONSTRAINT ' + @fk);

                    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SupplyItem_InventoryTypeSettingId')
                        DROP INDEX IX_SupplyItem_InventoryTypeSettingId ON SupplyItem;

                    ALTER TABLE SupplyItem DROP COLUMN InventoryTypeSettingId;
                END;
                """);

            await context.Database.ExecuteSqlRawAsync("""
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'InventoryTypeSetting')
                BEGIN
                    DROP TABLE InventoryTypeSetting;
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

            // InventoryItemDefinition/Variant/LocationInventorySafetyStock population now happens
            // entirely in BackfillInventoryDefinitionsFromSupplyItemsAsync (sourced from live
            // SupplyItem rows) - the InventoryTypeSetting-sourced backfill that used to run here
            // was removed along with the table it read from.
        }

        // Adds StockType (無效期物資/有效期物資/冷凍食品) to InventoryItemDefinition so 新增物資's
        // 物資種類/物資名稱/規格 dropdowns can be filtered by the StockType radio the same way
        // SupplyItem rows already are. Existing definitions get a best-guess StockType derived
        // from their SupplyItem rows' actual StockType (majority vote); that guess keeps
        // re-applying on every startup only until an admin edits the definition via the
        // 庫存種類設定 UI (InventoryTypeSettingController.Edit sets UpdatedAt), after which their
        // choice sticks and is never silently overwritten again.
        public static async Task EnsureInventoryItemDefinitionStockTypeAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await context.Database.ExecuteSqlRawAsync("""
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('InventoryItemDefinition') AND name = 'StockType')
                BEGIN
                    ALTER TABLE InventoryItemDefinition ADD StockType NVARCHAR(20) NOT NULL DEFAULT 'HasExpiry';
                END;
                """);

            await context.Database.ExecuteSqlRawAsync("""
                UPDATE d
                SET StockType = ranked.StockType
                FROM InventoryItemDefinition d
                CROSS APPLY (
                    SELECT TOP 1 si.StockType
                    FROM SupplyItem si
                    WHERE si.Category = d.Category AND si.ItemName = d.ItemName AND si.IsActive = 1
                    GROUP BY si.StockType
                    ORDER BY COUNT(*) DESC
                ) ranked
                WHERE d.UpdatedAt IS NULL;
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

        // Seeds the 物資種類目錄 the user supplied on 2026-08-10 (物資品項.xlsx, 39 物資名稱／54 規格
        // across 食品／生鮮冷凍食品／日用品／輔具). Idempotent and additive only:
        //   - A definition (Category+ItemName) is only INSERTed if that combination doesn't
        //     already exist (IsActive=1) - if it already exists (e.g. 食品/水 was already present
        //     from earlier test data with Unit=瓶/StockType=Frozen), its Unit/StockType is left
        //     untouched rather than silently overwritten with this preset's values, since we don't
        //     know whether real SupplyItem stock already depends on the existing definition.
        //   - A variant (Specification) is only INSERTed under whichever definition Id actually
        //     ended up existing (pre-existing or freshly inserted) if that spec isn't already there.
        // "無" is stored as a literal specification value (not NULL) for items without a
        // specification, per the user's explicit instruction, rather than the NULL-plus-"無規格"-
        // display convention used elsewhere (e.g. SupplyItem.Specification).
        // Two item names were split out of the source spreadsheet because their variants used
        // different minimum units, which this schema can't express within one definition
        // (Unit lives on InventoryItemDefinition, not InventoryItemVariant): 泡麵 -> 泡麵(袋裝)/
        // 泡麵(碗裝), 雞蛋 -> 雞蛋(盒裝)/雞蛋(箱裝). One duplicate source row (水 - 300ML／箱, listed
        // twice) was collapsed to one.
        public static async Task SeedPresetInventoryCatalogAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await context.Database.ExecuteSqlRawAsync("""
                DECLARE @NewDefinitions TABLE (Category NVARCHAR(50), ItemName NVARCHAR(100), Unit NVARCHAR(20), StockType NVARCHAR(20));
                INSERT INTO @NewDefinitions (Category, ItemName, Unit, StockType) VALUES
                (N'食品', N'米', N'包', N'HasExpiry'),
                (N'食品', N'水', N'箱', N'HasExpiry'),
                (N'食品', N'米酒', N'瓶', N'HasExpiry'),
                (N'食品', N'食用油', N'瓶', N'HasExpiry'),
                (N'食品', N'醬油', N'瓶', N'HasExpiry'),
                (N'食品', N'飲料', N'鋁箔包', N'HasExpiry'),
                (N'食品', N'罐頭', N'罐', N'HasExpiry'),
                (N'食品', N'綠豆', N'包', N'HasExpiry'),
                (N'食品', N'鹽巴', N'包', N'HasExpiry'),
                (N'食品', N'冬粉', N'包', N'HasExpiry'),
                (N'食品', N'米粉', N'包', N'HasExpiry'),
                (N'食品', N'麵條', N'包', N'HasExpiry'),
                (N'食品', N'泡麵(袋裝)', N'包', N'HasExpiry'),
                (N'食品', N'泡麵(碗裝)', N'碗', N'HasExpiry'),
                (N'生鮮冷凍食品', N'蔬菜', N'箱', N'Frozen'),
                (N'生鮮冷凍食品', N'豬肉', N'包', N'Frozen'),
                (N'生鮮冷凍食品', N'牛肉', N'包', N'Frozen'),
                (N'生鮮冷凍食品', N'雞肉', N'包', N'Frozen'),
                (N'生鮮冷凍食品', N'雞蛋(盒裝)', N'盒', N'HasExpiry'),
                (N'生鮮冷凍食品', N'雞蛋(箱裝)', N'箱', N'HasExpiry'),
                (N'生鮮冷凍食品', N'甜點類', N'包', N'Frozen'),
                (N'生鮮冷凍食品', N'湯類', N'包', N'Frozen'),
                (N'生鮮冷凍食品', N'沖泡類', N'包', N'HasExpiry'),
                (N'生鮮冷凍食品', N'麵包類', N'個', N'Frozen'),
                (N'日用品', N'成人紙尿布', N'包', N'NoExpiry'),
                (N'日用品', N'尿布墊', N'包', N'NoExpiry'),
                (N'日用品', N'棉被', N'條', N'NoExpiry'),
                (N'日用品', N'毯子', N'條', N'NoExpiry'),
                (N'日用品', N'床墊', N'座', N'NoExpiry'),
                (N'輔具', N'一般輪椅', N'台', N'NoExpiry'),
                (N'輔具', N'鐵製輪椅', N'台', N'NoExpiry'),
                (N'輔具', N'輕便輪椅', N'台', N'NoExpiry'),
                (N'輔具', N'高背輪椅', N'台', N'NoExpiry'),
                (N'輔具', N'便盆椅', N'座', N'NoExpiry'),
                (N'輔具', N'氣墊床', N'床', N'NoExpiry'),
                (N'輔具', N'電動床', N'座', N'NoExpiry'),
                (N'輔具', N'單拐', N'隻', N'NoExpiry'),
                (N'輔具', N'雙枴', N'隻', N'NoExpiry'),
                (N'輔具', N'四腳拐', N'組', N'NoExpiry');

                INSERT INTO InventoryItemDefinition (Category, ItemName, Unit, GlobalSafetyStock, IsActive, CreatedAt, StockType)
                SELECT nd.Category, nd.ItemName, nd.Unit, 0, 1, GETDATE(), nd.StockType
                FROM @NewDefinitions nd
                WHERE NOT EXISTS (
                    SELECT 1 FROM InventoryItemDefinition d
                    WHERE d.Category = nd.Category AND d.ItemName = nd.ItemName AND d.IsActive = 1
                );
                """);

            await context.Database.ExecuteSqlRawAsync("""
                DECLARE @NewVariants TABLE (Category NVARCHAR(50), ItemName NVARCHAR(100), Specification NVARCHAR(200));
                INSERT INTO @NewVariants (Category, ItemName, Specification) VALUES
                (N'食品', N'米', N'1公斤'),
                (N'食品', N'米', N'3公斤'),
                (N'食品', N'米', N'5公斤'),
                (N'食品', N'米', N'30公斤'),
                (N'食品', N'水', N'300ML'),
                (N'食品', N'水', N'600ML'),
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
                """);
        }

        // Seeds the 8 real 雲林縣智障者福利協進會 service centers the user pasted on 2026-08-10
        // (from https://www.yunfull.org.tw/OnePage.aspx?mid=20), so this data ships with the app
        // the same way SeedPresetInventoryCatalogAsync does - it re-applies on every startup and
        // on a fresh database, not just this machine's current copy.
        // Only LocationName/Address/Phone are seeded: the source page also had per-center Email
        // and a short service description, but SupplyLocation has no matching columns for those
        // today and the user chose not to add them for this pass - that richer info is not
        // stored anywhere and would need to be pulled from the source page again if wanted later.
        // Idempotent: a location is only inserted if no row with that exact LocationName exists
        // yet (regardless of IsActive, so re-activating one by hand isn't undone here).
        // Also deactivates (never deletes) the 4 placeholder test locations seeded during earlier
        // development (第一/第二/第三/第四物資據點, fake 05-0000001-style phone numbers) now that
        // real location data exists - existing SupplyItem/UserAccount rows that already reference
        // them by LocationId keep working untouched, they just stop appearing in "選擇據點" pickers.
        public static async Task SeedPresetLocationsAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await context.Database.ExecuteSqlRawAsync("""
                DECLARE @NewLocations TABLE (LocationName NVARCHAR(200), Address NVARCHAR(400), Phone NVARCHAR(60));
                INSERT INTO @NewLocations (LocationName, Address, Phone) VALUES
                (N'行政中心', N'雲林縣斗六市府文路30號', N'05-5341940'),
                (N'圓夢庇護工場', N'雲林縣斗六市保長路504號', N'05-5345467'),
                (N'雲林縣身心障礙者服務中心-斗六區', N'雲林縣斗六市府文路22號4樓', N'05-5362103'),
                (N'心歡喜日照中心', N'雲林縣斗六市南京路373號1樓', N'05-5372781'),
                (N'西螺服務據點', N'雲林縣西螺鎮光復西路286號', N'05-5873733'),
                (N'東勢服務中心', N'雲林縣東勢鄉東北村東勢東路395號', N'05-6993809'),
                (N'心圓寶日照中心', N'雲林縣北港鎮新街里穎寧街72號', N'05-7825113'),
                (N'北港服務中心', N'雲林縣北港鎮新街里新東街33巷8之3號', N'05-7827433');

                INSERT INTO SupplyLocation (LocationName, Address, Phone, IsActive, CreatedAt)
                SELECT nl.LocationName, nl.Address, nl.Phone, 1, GETDATE()
                FROM @NewLocations nl
                WHERE NOT EXISTS (
                    SELECT 1 FROM SupplyLocation sl WHERE sl.LocationName = nl.LocationName
                );

                UPDATE SupplyLocation
                SET IsActive = 0, UpdatedAt = GETDATE()
                WHERE LocationName IN (N'第一物資據點', N'第二物資據點', N'第三物資據點', N'第四物資據點')
                  AND IsActive = 1;
                """);
        }
    }
}

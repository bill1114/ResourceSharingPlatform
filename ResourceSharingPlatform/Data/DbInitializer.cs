using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ResourceSharingPlatform.Models;

namespace ResourceSharingPlatform.Data
{
    public static class DbInitializer
    {
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
    }
}

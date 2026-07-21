using Microsoft.AspNetCore.Identity;
using ResourceSharingPlatform.Models;

namespace ResourceSharingPlatform.Services.GoogleSheets
{
    // Replaces Data/DbInitializer.cs. Same logic, just backed by the Sheets
    // store instead of ApplicationDbContext.
    public static class AdminSeeder
    {
        public static async Task SeedAdminAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<SheetsDataStore>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<UserAccount>>();

            var existingUsers = await store.GetUsersAsync();
            if (existingUsers.Count > 0)
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

            await store.CreateUserAsync(admin);
        }
    }
}

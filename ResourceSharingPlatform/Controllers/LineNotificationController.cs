using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResourceSharingPlatform.Data;
using ResourceSharingPlatform.Models;
using ResourceSharingPlatform.Models.ViewModels;

namespace ResourceSharingPlatform.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class LineNotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LineNotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: LineNotification/Settings
        public async Task<IActionResult> Settings()
        {
            var settings = await GetSettingsAsync();

            var model = new LineNotificationSettingsViewModel
            {
                IsEnabled = settings.IsEnabled,
                NotifyLowStock = settings.NotifyLowStock,
                NotifyExpiringSoon = settings.NotifyExpiringSoon,
                NotifyExpired = settings.NotifyExpired
            };

            await PopulateViewBagAsync(settings);

            return View(model);
        }

        // POST: LineNotification/Settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(LineNotificationSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                var settings = await GetSettingsAsync();

                settings.IsEnabled = model.IsEnabled;
                settings.NotifyLowStock = model.NotifyLowStock;
                settings.NotifyExpiringSoon = model.NotifyExpiringSoon;
                settings.NotifyExpired = model.NotifyExpired;

                if (!string.IsNullOrWhiteSpace(model.ChannelAccessToken))
                {
                    settings.ChannelAccessToken = model.ChannelAccessToken;
                }

                if (!string.IsNullOrWhiteSpace(model.ChannelSecret))
                {
                    settings.ChannelSecret = model.ChannelSecret;
                }

                settings.UpdatedAt = DateTime.Now;
                settings.UpdatedBy = User.FindFirstValue("DisplayName") ?? User.Identity?.Name;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "LINE 通知設定已儲存";
                return RedirectToAction(nameof(Settings));
            }

            var current = await GetSettingsAsync();
            await PopulateViewBagAsync(current);
            return View(model);
        }

        // POST: LineNotification/TestSend
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestSend()
        {
            var settings = await GetSettingsAsync();
            var previewItems = await GetPreviewItemsAsync(settings);

            TempData["SuccessMessage"] =
                $"（模擬）已模擬產生 {previewItems.Count} 則通知內容，尚未串接真實 LINE Messaging API，不會真的傳送。";

            return RedirectToAction(nameof(Settings));
        }

        private async Task<LineNotificationSettings> GetSettingsAsync()
        {
            var settings = await _context.LineNotificationSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new LineNotificationSettings();
                _context.LineNotificationSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return settings;
        }

        private async Task PopulateViewBagAsync(LineNotificationSettings settings)
        {
            ViewBag.HasToken = !string.IsNullOrEmpty(settings.ChannelAccessToken);
            ViewBag.HasSecret = !string.IsNullOrEmpty(settings.ChannelSecret);
            ViewBag.UpdatedAt = settings.UpdatedAt;
            ViewBag.UpdatedBy = settings.UpdatedBy;
            ViewBag.PreviewItems = await GetPreviewItemsAsync(settings);
        }

        private async Task<List<SupplyItem>> GetPreviewItemsAsync(LineNotificationSettings settings)
        {
            var today = DateTime.Today;
            var expiringDate = today.AddDays(30);

            var items = await _context.SupplyItems
                .Include(x => x.Location)
                .Where(x => x.IsActive)
                .ToListAsync();

            return items.Where(x =>
                (settings.NotifyLowStock && x.Quantity <= x.SafetyStock) ||
                (settings.NotifyExpired && x.ExpirationDate.HasValue && x.ExpirationDate.Value < today) ||
                (settings.NotifyExpiringSoon && x.ExpirationDate.HasValue && x.ExpirationDate.Value >= today && x.ExpirationDate.Value <= expiringDate)
            ).ToList();
        }
    }
}

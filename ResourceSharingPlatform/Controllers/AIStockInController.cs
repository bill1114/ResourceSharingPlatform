using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ResourceSharingPlatform.Data;
using ResourceSharingPlatform.Models;
using ResourceSharingPlatform.Models.ViewModels;
using ResourceSharingPlatform.Services;

namespace ResourceSharingPlatform.Controllers
{
    public class AIStockInController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AIStockInService _aiStockInService;

        public AIStockInController(ApplicationDbContext context, AIStockInService aiStockInService)
        {
            _context = context;
            _aiStockInService = aiStockInService;
        }

        // GET: AIStockIn
        public async Task<IActionResult> Index(string? keyword)
        {
            var query = _context.AIStockInLogs
                .Include(x => x.Location)
                .Include(x => x.ConfirmedSupplyItem)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    (x.SuggestedItemName != null && x.SuggestedItemName.Contains(keyword)) ||
                    (x.InputText != null && x.InputText.Contains(keyword)) ||
                    (x.Operator != null && x.Operator.Contains(keyword)));
            }

            var logs = await query
                .OrderByDescending(x => x.CreatedAt)
                .Take(100)
                .ToListAsync();

            ViewBag.Keyword = keyword;

            return View(logs);
        }

        // GET: AIStockIn/Create
        [Authorize(Roles = Roles.AdminAndCadre)]
        public async Task<IActionResult> Create()
        {
            var settings = await _context.AIStockInSettings.FirstOrDefaultAsync();
            ViewBag.Settings = settings;
            ViewBag.Locations = new SelectList(
                await _context.SupplyLocations.Where(x => x.IsActive).ToListAsync(),
                "Id",
                "LocationName"
            );

            return View();
        }

        // POST: AIStockIn/Recognize
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdminAndCadre)]
        public async Task<IActionResult> Recognize(int locationId, string inputType, string? inputText, IFormFile? image)
        {
            if (locationId <= 0)
            {
                ModelState.AddModelError(string.Empty, "請選擇據點");
            }

            if (inputType == AIStockInInputTypes.Image && (image == null || image.Length == 0))
            {
                ModelState.AddModelError(string.Empty, "請上傳照片");
            }
            else if (inputType == AIStockInInputTypes.Text && string.IsNullOrWhiteSpace(inputText))
            {
                ModelState.AddModelError(string.Empty, "請輸入文字描述");
            }

            if (image != null && image.Length > 0 && !_aiStockInService.TryValidateImage(image, out var imageError))
            {
                ModelState.AddModelError(string.Empty, imageError!);
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = string.Join("；", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Create));
            }

            var operatorName = User.FindFirstValue("DisplayName") ?? User.Identity?.Name;
            var log = await _aiStockInService.RecognizeAsync(locationId, inputType, inputText, image, operatorName);

            return RedirectToAction(nameof(Confirm), new { id = log.Id });
        }

        // GET: AIStockIn/Confirm/5
        [Authorize(Roles = Roles.AdminAndCadre)]
        public async Task<IActionResult> Confirm(int id)
        {
            var log = await _context.AIStockInLogs
                .Include(x => x.Location)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (log == null)
            {
                return NotFound();
            }

            if (log.IsConfirmed)
            {
                TempData["ErrorMessage"] = "此筆辨識紀錄已經確認過了";
                return RedirectToAction(nameof(Index));
            }

            var model = new AIStockInViewModel
            {
                LocationId = log.LocationId,
                Category = log.SuggestedCategory ?? string.Empty,
                ItemName = log.SuggestedItemName ?? string.Empty,
                Specification = log.SuggestedSpecification,
                Quantity = log.SuggestedQuantity ?? 0,
                Unit = log.SuggestedUnit,
                StockType = log.SuggestedStockType ?? StockTypes.HasExpiry,
                ExpirationDate = log.SuggestedExpirationDate,
                SafetyStock = log.SuggestedSafetyStock ?? 0,
                Remark = log.SuggestedRemark,
                SourceInputType = log.InputType,
                SourceInputText = log.InputText,
                Confidence = log.Confidence
            };

            await PopulateLocationsAsync();
            ViewBag.Log = log;

            return View(model);
        }

        // POST: AIStockIn/Confirm/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdminAndCadre)]
        public async Task<IActionResult> Confirm(int id, AIStockInViewModel model)
        {
            var log = await _context.AIStockInLogs
                .Include(x => x.Location)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (log == null)
            {
                return NotFound();
            }

            if (log.IsConfirmed)
            {
                TempData["ErrorMessage"] = "此筆辨識紀錄已經確認過了";
                return RedirectToAction(nameof(Index));
            }

            if (model.StockType == StockTypes.HasExpiry || model.StockType == StockTypes.Frozen)
            {
                if (model.ExpirationDate == null)
                {
                    ModelState.AddModelError(nameof(model.ExpirationDate), "此分類必須填寫有效期限");
                }
            }
            else
            {
                model.ExpirationDate = null;
            }

            if (ModelState.IsValid)
            {
                var existingMatch = await _context.SupplyItems.FirstOrDefaultAsync(x =>
                    x.IsActive &&
                    x.LocationId == model.LocationId &&
                    x.ItemName == model.ItemName &&
                    x.Category == model.Category &&
                    x.Specification == model.Specification &&
                    x.StockType == model.StockType &&
                    x.ExpirationDate == model.ExpirationDate);

                SupplyItem resultItem;

                if (existingMatch != null)
                {
                    existingMatch.Quantity += model.Quantity;
                    existingMatch.UpdatedAt = DateTime.Now;
                    resultItem = existingMatch;
                }
                else
                {
                    resultItem = new SupplyItem
                    {
                        Category = model.Category,
                        ItemName = model.ItemName,
                        Specification = model.Specification,
                        Quantity = model.Quantity,
                        Unit = model.Unit,
                        StockType = model.StockType,
                        ExpirationDate = model.ExpirationDate,
                        LocationId = model.LocationId,
                        SafetyStock = model.SafetyStock,
                        Remark = model.Remark,
                        ImagePath = log.InputImagePath,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    _context.SupplyItems.Add(resultItem);
                }

                await _context.SaveChangesAsync();

                log.IsConfirmed = true;
                log.ConfirmedSupplyItemId = resultItem.Id;
                log.ConfirmedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = existingMatch != null
                    ? $"據點已有相同物資，已合併數量，目前數量為 {resultItem.Quantity} {resultItem.Unit}"
                    : "已確認 AI 辨識結果並建立物資！";
                return RedirectToAction("Index", "SupplyItem");
            }

            await PopulateLocationsAsync();
            ViewBag.Log = log;
            return View(model);
        }

        // GET/POST: AIStockIn/Settings
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Settings()
        {
            var settings = await GetSettingsAsync();

            var model = new AIStockInSettingsViewModel
            {
                IsEnabled = settings.IsEnabled,
                ApiEndpoint = settings.ApiEndpoint,
                SupportsImageInput = settings.SupportsImageInput,
                SupportsTextInput = settings.SupportsTextInput
            };

            await PopulateSettingsViewBagAsync(settings);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Settings(AIStockInSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                var settings = await GetSettingsAsync();

                settings.IsEnabled = model.IsEnabled;
                settings.ApiEndpoint = model.ApiEndpoint;
                settings.SupportsImageInput = model.SupportsImageInput;
                settings.SupportsTextInput = model.SupportsTextInput;

                if (!string.IsNullOrWhiteSpace(model.ApiKey))
                {
                    settings.ApiKey = model.ApiKey;
                }

                settings.UpdatedAt = DateTime.Now;
                settings.UpdatedBy = User.FindFirstValue("DisplayName") ?? User.Identity?.Name;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "AI 智慧入庫設定已儲存";
                return RedirectToAction(nameof(Settings));
            }

            var current = await GetSettingsAsync();
            await PopulateSettingsViewBagAsync(current);
            return View(model);
        }

        private async Task<AIStockInSettings> GetSettingsAsync()
        {
            var settings = await _context.AIStockInSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new AIStockInSettings();
                _context.AIStockInSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return settings;
        }

        private Task PopulateSettingsViewBagAsync(AIStockInSettings settings)
        {
            ViewBag.HasApiKey = !string.IsNullOrEmpty(settings.ApiKey);
            ViewBag.UpdatedAt = settings.UpdatedAt;
            ViewBag.UpdatedBy = settings.UpdatedBy;
            return Task.CompletedTask;
        }

        private async Task PopulateLocationsAsync()
        {
            ViewBag.Locations = new SelectList(
                await _context.SupplyLocations.Where(x => x.IsActive).ToListAsync(),
                "Id",
                "LocationName"
            );
        }
    }
}

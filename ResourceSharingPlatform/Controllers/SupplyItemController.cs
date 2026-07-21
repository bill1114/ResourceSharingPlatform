using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ResourceSharingPlatform.Models;
using ResourceSharingPlatform.Services.GoogleSheets;

namespace ResourceSharingPlatform.Controllers
{
    public class SupplyItemController : Controller
    {
        private readonly SheetsDataStore _store;
        private readonly IWebHostEnvironment _environment;

        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxImageSizeBytes = 5 * 1024 * 1024;

        public SupplyItemController(SheetsDataStore store, IWebHostEnvironment environment)
        {
            _store = store;
            _environment = environment;
        }

        // GET: SupplyItem
        public async Task<IActionResult> Index(int? locationId, string? category, string? stockType)
        {
            var query = (await _store.GetItemsAsync())
                .Where(x => x.IsActive)
                .AsEnumerable();

            if (locationId.HasValue)
            {
                query = query.Where(x => x.LocationId == locationId.Value);
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(x => x.Category == category);
            }

            if (!string.IsNullOrEmpty(stockType))
            {
                query = query.Where(x => x.StockType == stockType);
            }

            var items = query.OrderByDescending(x => x.CreatedAt).ToList();

            // For filter dropdowns
            ViewBag.Locations = new SelectList(
                (await _store.GetLocationsAsync()).Where(x => x.IsActive).ToList(),
                "Id",
                "LocationName",
                locationId
            );

            ViewBag.Categories = (await _store.GetItemsAsync())
                .Where(x => x.IsActive)
                .Select(x => x.Category)
                .Distinct()
                .ToList();

            ViewBag.SelectedCategory = category;
            ViewBag.SelectedStockType = stockType;

            return View(items);
        }

        // GET: SupplyItem/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _store.GetItemByIdAsync(id.Value);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // GET: SupplyItem/Create
        [Authorize(Roles = Roles.AdminAndCadre)]
        public async Task<IActionResult> Create()
        {
            ViewBag.Locations = new SelectList(
                (await _store.GetLocationsAsync()).Where(x => x.IsActive).ToList(),
                "Id",
                "LocationName"
            );
            return View();
        }

        // POST: SupplyItem/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdminAndCadre)]
        public async Task<IActionResult> Create(
            [Bind("Category,ItemName,Specification,Quantity,Unit,StockType,ExpirationDate,LocationId,SafetyStock,Remark")] SupplyItem item,
            IFormFile? imageFile)
        {
            if (item.StockType == StockTypes.HasExpiry || item.StockType == StockTypes.Frozen)
            {
                if (item.ExpirationDate == null)
                {
                    ModelState.AddModelError(nameof(item.ExpirationDate), "此分類必須填寫有效期限");
                }
            }
            else
            {
                item.ExpirationDate = null;
            }

            if (imageFile != null && imageFile.Length > 0 && !TryValidateImage(imageFile, out var imageError))
            {
                ModelState.AddModelError(string.Empty, imageError!);
            }

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    item.ImagePath = await SaveImageAsync(imageFile);
                }

                item.IsActive = true;
                item.CreatedAt = DateTime.Now;
                await _store.CreateItemAsync(item);
                TempData["SuccessMessage"] = "物資新增成功！";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Locations = new SelectList(
                (await _store.GetLocationsAsync()).Where(x => x.IsActive).ToList(),
                "Id",
                "LocationName",
                item.LocationId
            );
            return View(item);
        }

        // GET: SupplyItem/Edit/5
        [Authorize(Roles = Roles.AdminAndCadre)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _store.GetItemByIdAsync(id.Value);
            if (item == null)
            {
                return NotFound();
            }

            ViewBag.Locations = new SelectList(
                (await _store.GetLocationsAsync()).Where(x => x.IsActive).ToList(),
                "Id",
                "LocationName",
                item.LocationId
            );
            return View(item);
        }

        // POST: SupplyItem/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdminAndCadre)]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Category,ItemName,Specification,Quantity,Unit,StockType,ExpirationDate,LocationId,SafetyStock,Remark,IsActive,CreatedAt")] SupplyItem item,
            IFormFile? imageFile)
        {
            if (id != item.Id)
            {
                return NotFound();
            }

            if (item.StockType == StockTypes.HasExpiry || item.StockType == StockTypes.Frozen)
            {
                if (item.ExpirationDate == null)
                {
                    ModelState.AddModelError(nameof(item.ExpirationDate), "此分類必須填寫有效期限");
                }
            }
            else
            {
                item.ExpirationDate = null;
            }

            if (imageFile != null && imageFile.Length > 0 && !TryValidateImage(imageFile, out var imageError))
            {
                ModelState.AddModelError(string.Empty, imageError!);
            }

            if (ModelState.IsValid)
            {
                var existing = await _store.GetItemByIdAsync(id);
                item.ImagePath = existing?.ImagePath;

                if (imageFile != null && imageFile.Length > 0)
                {
                    DeleteImageIfExists(existing?.ImagePath);
                    item.ImagePath = await SaveImageAsync(imageFile);
                }

                item.UpdatedAt = DateTime.Now;
                await _store.UpdateItemAsync(item);
                TempData["SuccessMessage"] = "物資更新成功！";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Locations = new SelectList(
                (await _store.GetLocationsAsync()).Where(x => x.IsActive).ToList(),
                "Id",
                "LocationName",
                item.LocationId
            );
            return View(item);
        }

        // GET: SupplyItem/Delete/5
        [Authorize(Roles = Roles.AdminAndCadre)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _store.GetItemByIdAsync(id.Value);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // POST: SupplyItem/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdminAndCadre)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _store.GetItemByIdAsync(id);
            if (item != null)
            {
                item.IsActive = false;
                item.UpdatedAt = DateTime.Now;
                await _store.UpdateItemAsync(item);
                TempData["SuccessMessage"] = "物資已停用！";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TryValidateImage(IFormFile file, out string? error)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(ext))
            {
                error = "圖片格式僅支援 jpg、jpeg、png、webp";
                return false;
            }

            if (file.Length > MaxImageSizeBytes)
            {
                error = "圖片大小不可超過 5MB";
                return false;
            }

            error = null;
            return true;
        }

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "items");
            Directory.CreateDirectory(uploadsFolder);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = Guid.NewGuid().ToString("N") + ext;
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return "/uploads/items/" + fileName;
        }

        private void DeleteImageIfExists(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return;
            }

            try
            {
                var fullPath = Path.Combine(_environment.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch
            {
                // Best-effort cleanup; ignore failures.
            }
        }
    }
}

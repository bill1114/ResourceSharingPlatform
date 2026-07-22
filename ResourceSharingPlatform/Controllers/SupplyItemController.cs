using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ResourceSharingPlatform.Data;
using ResourceSharingPlatform.Models;
using ResourceSharingPlatform.Models.ViewModels;

namespace ResourceSharingPlatform.Controllers
{
    public class SupplyItemController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxImageSizeBytes = 5 * 1024 * 1024;

        public SupplyItemController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: SupplyItem
        public async Task<IActionResult> Index(int? locationId, string? category, string? stockType, string? keyword)
        {
            var query = _context.SupplyItems
                .Include(s => s.Location)
                .Where(x => x.IsActive);

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

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.ItemName.Contains(keyword) ||
                    (x.Specification != null && x.Specification.Contains(keyword)) ||
                    x.Category.Contains(keyword) ||
                    (x.Remark != null && x.Remark.Contains(keyword)));
            }

            var items = (await query.ToListAsync())
                .OrderBy(x => x.ExpirationDate.HasValue ? 0 : 1)
                .ThenBy(x => x.ExpirationDate)
                .ThenByDescending(x => x.CreatedAt)
                .ToList();

            // For filter dropdowns
            ViewBag.Locations = new SelectList(
                await _context.SupplyLocations.Where(x => x.IsActive).ToListAsync(),
                "Id",
                "LocationName",
                locationId
            );

            ViewBag.Categories = await _context.SupplyItems
                .Where(x => x.IsActive)
                .Select(x => x.Category)
                .Distinct()
                .ToListAsync();

            ViewBag.SelectedCategory = category;
            ViewBag.SelectedStockType = stockType;
            ViewBag.Keyword = keyword;

            var today = DateTime.Today;
            ViewBag.ItemSummary = items
                .GroupBy(x => x.ItemName)
                .Select(g => new SupplyItemSummaryViewModel
                {
                    ItemName = g.Key,
                    LocationCount = g.Select(x => x.LocationId).Distinct().Count(),
                    TotalQuantity = g.Sum(x => x.Quantity),
                    Unit = g.First().Unit,
                    HasLowStock = g.Any(x => x.Quantity <= x.SafetyStock),
                    NearestExpirationDate = g.Where(x => x.ExpirationDate.HasValue).Select(x => x.ExpirationDate).OrderBy(d => d).FirstOrDefault()
                })
                .OrderBy(x => x.NearestExpirationDate.HasValue ? 0 : 1)
                .ThenBy(x => x.NearestExpirationDate)
                .ThenByDescending(x => x.TotalQuantity)
                .ToList();

            return View(items);
        }

        // GET: SupplyItem/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.SupplyItems
                .Include(s => s.Location)
                .FirstOrDefaultAsync(m => m.Id == id);

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
                await _context.SupplyLocations.Where(x => x.IsActive).ToListAsync(),
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
                var existingMatch = await _context.SupplyItems.FirstOrDefaultAsync(x =>
                    x.IsActive &&
                    x.LocationId == item.LocationId &&
                    x.ItemName == item.ItemName &&
                    x.Category == item.Category &&
                    x.Specification == item.Specification &&
                    x.StockType == item.StockType &&
                    x.ExpirationDate == item.ExpirationDate);

                if (existingMatch != null)
                {
                    existingMatch.Quantity += item.Quantity;
                    existingMatch.UpdatedAt = DateTime.Now;

                    if (imageFile != null && imageFile.Length > 0 && string.IsNullOrEmpty(existingMatch.ImagePath))
                    {
                        existingMatch.ImagePath = await SaveImageAsync(imageFile);
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"據點已有相同物資，已合併數量，目前數量為 {existingMatch.Quantity} {existingMatch.Unit}";
                    return RedirectToAction(nameof(Index));
                }

                if (imageFile != null && imageFile.Length > 0)
                {
                    item.ImagePath = await SaveImageAsync(imageFile);
                }

                item.IsActive = true;
                item.CreatedAt = DateTime.Now;
                _context.Add(item);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "物資新增成功！";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Locations = new SelectList(
                await _context.SupplyLocations.Where(x => x.IsActive).ToListAsync(),
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

            var item = await _context.SupplyItems.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            ViewBag.Locations = new SelectList(
                await _context.SupplyLocations.Where(x => x.IsActive).ToListAsync(),
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
                try
                {
                    var existing = await _context.SupplyItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                    item.ImagePath = existing?.ImagePath;

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        DeleteImageIfExists(existing?.ImagePath);
                        item.ImagePath = await SaveImageAsync(imageFile);
                    }

                    item.UpdatedAt = DateTime.Now;
                    _context.Update(item);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "物資更新成功！";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ItemExists(item.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Locations = new SelectList(
                await _context.SupplyLocations.Where(x => x.IsActive).ToListAsync(),
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

            var item = await _context.SupplyItems
                .Include(s => s.Location)
                .FirstOrDefaultAsync(m => m.Id == id);

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
            var item = await _context.SupplyItems.FindAsync(id);
            if (item != null)
            {
                item.IsActive = false;
                item.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "物資已停用！";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ItemExists(int id)
        {
            return _context.SupplyItems.Any(e => e.Id == id);
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

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResourceSharingPlatform.Data;
using ResourceSharingPlatform.Models;
using ResourceSharingPlatform.Models.ViewModels;

namespace ResourceSharingPlatform.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class InventoryTypeSettingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InventoryTypeSettingController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? keyword)
        {
            var query = _context.InventoryItemDefinitions
                .Include(x => x.Variants)
                .Include(x => x.LocationSafetyStocks)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x => x.Category.Contains(keyword) || x.ItemName.Contains(keyword));
            }

            ViewBag.Keyword = keyword;
            return View(await query
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.Category)
                .ThenBy(x => x.ItemName)
                .ToListAsync());
        }

        public IActionResult Create() => View(new InventoryDefinitionFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InventoryDefinitionFormViewModel model)
        {
            Normalize(model);
            await ValidateDuplicateDefinitionAsync(model.Category, model.ItemName);
            if (!ModelState.IsValid) return View(model);

            var definition = new InventoryItemDefinition
            {
                Category = model.Category,
                ItemName = model.ItemName,
                Unit = model.Unit,
                GlobalSafetyStock = model.GlobalSafetyStock,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            definition.Variants.Add(new InventoryItemVariant
            {
                Specification = NormalizeOptional(model.InitialSpecification),
                IsActive = true,
                CreatedAt = DateTime.Now
            });

            _context.InventoryItemDefinitions.Add(definition);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "庫存種類已新增，請繼續設定各據點安全庫存。";
            return RedirectToAction(nameof(LocationSafety), new { id = definition.Id });
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!id.HasValue) return NotFound();
            var definition = await _context.InventoryItemDefinitions.FindAsync(id.Value);
            if (definition == null) return NotFound();

            return View(new InventoryDefinitionFormViewModel
            {
                Id = definition.Id,
                Category = definition.Category,
                ItemName = definition.ItemName,
                Unit = definition.Unit,
                GlobalSafetyStock = definition.GlobalSafetyStock,
                IsActive = definition.IsActive
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InventoryDefinitionFormViewModel model)
        {
            if (id != model.Id) return NotFound();
            var definition = await _context.InventoryItemDefinitions.FindAsync(id);
            if (definition == null) return NotFound();

            Normalize(model);
            await ValidateDuplicateDefinitionAsync(model.Category, model.ItemName, id);
            if (!ModelState.IsValid) return View(model);

            definition.Category = model.Category;
            definition.ItemName = model.ItemName;
            definition.Unit = model.Unit;
            definition.GlobalSafetyStock = model.GlobalSafetyStock;
            definition.IsActive = model.IsActive;
            definition.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "庫存種類已更新。";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Variants(int id)
        {
            var definition = await _context.InventoryItemDefinitions
                .Include(x => x.Variants)
                .FirstOrDefaultAsync(x => x.Id == id);
            return definition == null ? NotFound() : View(definition);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVariant(int id, string? specification)
        {
            var definition = await _context.InventoryItemDefinitions.FindAsync(id);
            if (definition == null) return NotFound();

            specification = NormalizeOptional(specification);
            var duplicate = await _context.InventoryItemVariants.AnyAsync(x =>
                x.InventoryItemDefinitionId == id && x.IsActive && x.Specification == specification);
            if (duplicate)
            {
                TempData["ErrorMessage"] = "相同規格已存在。";
                return RedirectToAction(nameof(Variants), new { id });
            }

            _context.InventoryItemVariants.Add(new InventoryItemVariant
            {
                InventoryItemDefinitionId = id,
                Specification = specification,
                IsActive = true,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "規格已新增。";
            return RedirectToAction(nameof(Variants), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableVariant(int id)
        {
            var variant = await _context.InventoryItemVariants.FindAsync(id);
            if (variant == null) return NotFound();
            variant.IsActive = false;
            variant.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "規格已停用，既有庫存資料不受影響。";
            return RedirectToAction(nameof(Variants), new { id = variant.InventoryItemDefinitionId });
        }

        public async Task<IActionResult> LocationSafety(int id)
        {
            var definition = await _context.InventoryItemDefinitions.FindAsync(id);
            if (definition == null) return NotFound();

            var locations = await _context.SupplyLocations.Where(x => x.IsActive).OrderBy(x => x.LocationName).ToListAsync();
            var existing = await _context.LocationInventorySafetyStocks
                .Where(x => x.InventoryItemDefinitionId == id)
                .ToDictionaryAsync(x => x.LocationId, x => x.SafetyStock);

            return View(new LocationSafetyStockFormViewModel
            {
                DefinitionId = id,
                DefinitionName = definition.DisplayName,
                Unit = definition.Unit,
                Locations = locations.Select(x => new LocationSafetyStockLineViewModel
                {
                    LocationId = x.Id,
                    LocationName = x.LocationName,
                    SafetyStock = existing.GetValueOrDefault(x.Id)
                }).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LocationSafety(LocationSafetyStockFormViewModel model)
        {
            var definition = await _context.InventoryItemDefinitions.FindAsync(model.DefinitionId);
            if (definition == null) return NotFound();
            if (!ModelState.IsValid)
            {
                model.DefinitionName = definition.DisplayName;
                model.Unit = definition.Unit;
                return View(model);
            }

            var existing = await _context.LocationInventorySafetyStocks
                .Where(x => x.InventoryItemDefinitionId == model.DefinitionId)
                .ToDictionaryAsync(x => x.LocationId);

            foreach (var line in model.Locations)
            {
                if (existing.TryGetValue(line.LocationId, out var setting))
                {
                    setting.SafetyStock = line.SafetyStock;
                    setting.UpdatedAt = DateTime.Now;
                }
                else
                {
                    _context.LocationInventorySafetyStocks.Add(new LocationInventorySafetyStock
                    {
                        LocationId = line.LocationId,
                        InventoryItemDefinitionId = model.DefinitionId,
                        SafetyStock = line.SafetyStock,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "各據點安全庫存已更新。";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disable(int id)
        {
            var definition = await _context.InventoryItemDefinitions.FindAsync(id);
            if (definition == null) return NotFound();
            definition.IsActive = false;
            definition.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "庫存種類已停用。";
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateDuplicateDefinitionAsync(string category, string itemName, int? excludedId = null)
        {
            var duplicate = await _context.InventoryItemDefinitions.AnyAsync(x =>
                x.IsActive && x.Id != excludedId && x.Category == category && x.ItemName == itemName);
            if (duplicate) ModelState.AddModelError(string.Empty, "相同物資種類與名稱已存在。");
        }

        private static void Normalize(InventoryDefinitionFormViewModel model)
        {
            model.Category = model.Category.Trim();
            model.ItemName = model.ItemName.Trim();
            model.Unit = model.Unit.Trim();
            model.InitialSpecification = NormalizeOptional(model.InitialSpecification);
        }

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

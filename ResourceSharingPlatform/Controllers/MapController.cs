using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResourceSharingPlatform.Data;
using ResourceSharingPlatform.Models.ViewModels;

namespace ResourceSharingPlatform.Controllers
{
    public class MapController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MapController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetLocations()
        {
            var today = DateTime.Today;
            var expiringDate = today.AddDays(30);

            var locations = await _context.SupplyLocations
                .Where(x => x.IsActive && x.Latitude != null && x.Longitude != null)
                .ToListAsync();

            var items = await _context.SupplyItems
                .Where(x => x.IsActive)
                .ToListAsync();

            var definitions = await _context.InventoryItemDefinitions.Where(x => x.IsActive).ToListAsync();
            var variants = await _context.InventoryItemVariants.Where(x => x.IsActive).ToListAsync();
            var safetySettings = await _context.LocationInventorySafetyStocks.Where(x => x.SafetyStock > 0).ToListAsync();
            var definitionsById = definitions.ToDictionary(x => x.Id);
            var definitionByVariantId = variants
                .Where(x => definitionsById.ContainsKey(x.InventoryItemDefinitionId))
                .ToDictionary(x => x.Id, x => x.InventoryItemDefinitionId);
            var definitionByName = definitions
                .GroupBy(x => (x.Category, x.ItemName))
                .ToDictionary(x => x.Key, x => x.First().Id);

            int? ResolveDefinitionId(ResourceSharingPlatform.Models.SupplyItem item)
            {
                if (item.InventoryItemVariantId.HasValue &&
                    definitionByVariantId.TryGetValue(item.InventoryItemVariantId.Value, out var variantDefinitionId))
                {
                    return variantDefinitionId;
                }
                return definitionByName.TryGetValue((item.Category, item.ItemName), out var nameDefinitionId)
                    ? nameDefinitionId
                    : null;
            }

            var resolvedItems = items.Select(x => new { Item = x, DefinitionId = ResolveDefinitionId(x) }).ToList();
            var locationTotals = resolvedItems.Where(x => x.DefinitionId.HasValue)
                .GroupBy(x => (x.Item.LocationId, x.DefinitionId!.Value))
                .ToDictionary(x => x.Key, x => x.Sum(y => y.Item.Quantity));

            var result = locations.Select(l => new MapLocationViewModel
            {
                LocationId = l.Id,
                LocationName = l.LocationName,
                Address = l.Address,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                ItemTypeCount = resolvedItems.Where(i => i.Item.LocationId == l.Id && i.DefinitionId.HasValue)
                    .Select(i => i.DefinitionId).Distinct().Count(),
                TotalQuantity = items.Where(i => i.LocationId == l.Id).Sum(i => i.Quantity),
                LowStockCount = safetySettings.Count(s => s.LocationId == l.Id &&
                    locationTotals.GetValueOrDefault((l.Id, s.InventoryItemDefinitionId)) <= s.SafetyStock),
                ExpiringSoonCount = items.Count(i => i.LocationId == l.Id && i.ExpirationDate != null && i.ExpirationDate >= today && i.ExpirationDate <= expiringDate)
            }).ToList();

            return Json(result);
        }
    }
}

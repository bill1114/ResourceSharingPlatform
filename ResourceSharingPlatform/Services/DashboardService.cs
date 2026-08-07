using Microsoft.EntityFrameworkCore;
using ResourceSharingPlatform.Data;
using ResourceSharingPlatform.Models;
using ResourceSharingPlatform.Models.ViewModels;

namespace ResourceSharingPlatform.Services
{
    public class DashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardAsync()
        {
            var today = DateTime.Today;
            var expiringDate = today.AddDays(30);

            var items = await _context.SupplyItems.Include(x => x.Location).Where(x => x.IsActive).ToListAsync();
            var locations = await _context.SupplyLocations.Where(x => x.IsActive).ToListAsync();
            var definitions = await _context.InventoryItemDefinitions.Where(x => x.IsActive).ToListAsync();
            var variants = await _context.InventoryItemVariants.Where(x => x.IsActive).ToListAsync();
            var locationSafetyStocks = await _context.LocationInventorySafetyStocks.ToListAsync();

            var definitionsById = definitions.ToDictionary(x => x.Id);
            var definitionByVariantId = variants
                .Where(x => definitionsById.ContainsKey(x.InventoryItemDefinitionId))
                .ToDictionary(x => x.Id, x => definitionsById[x.InventoryItemDefinitionId]);
            var definitionByName = definitions
                .GroupBy(x => (x.Category, x.ItemName))
                .ToDictionary(x => x.Key, x => x.First());

            InventoryItemDefinition? ResolveDefinition(SupplyItem item)
            {
                if (item.InventoryItemVariantId.HasValue &&
                    definitionByVariantId.TryGetValue(item.InventoryItemVariantId.Value, out var byVariant))
                {
                    return byVariant;
                }
                return definitionByName.GetValueOrDefault((item.Category, item.ItemName));
            }

            var resolvedItems = items
                .Select(x => new { Item = x, Definition = ResolveDefinition(x) })
                .Where(x => x.Definition != null)
                .ToList();

            var locationTotals = resolvedItems
                .GroupBy(x => (x.Item.LocationId, x.Definition!.Id))
                .ToDictionary(x => x.Key, x => x.Sum(y => y.Item.Quantity));

            var locationLowStocks = locationSafetyStocks
                .Where(x => x.SafetyStock > 0 && definitionsById.ContainsKey(x.InventoryItemDefinitionId))
                .Select(x =>
                {
                    var definition = definitionsById[x.InventoryItemDefinitionId];
                    var location = locations.FirstOrDefault(l => l.Id == x.LocationId);
                    return new LocationLowStockItem
                    {
                        LocationId = x.LocationId,
                        LocationName = location?.LocationName ?? "停用據點",
                        DefinitionId = definition.Id,
                        Category = definition.Category,
                        ItemName = definition.ItemName,
                        Unit = definition.Unit,
                        TotalQuantity = locationTotals.GetValueOrDefault((x.LocationId, definition.Id)),
                        SafetyStock = x.SafetyStock
                    };
                })
                .Where(x => x.TotalQuantity <= x.SafetyStock)
                .OrderByDescending(x => x.Shortage)
                .ThenBy(x => x.LocationName)
                .ThenBy(x => x.ItemName)
                .ToList();

            var storedTotals = resolvedItems
                .GroupBy(x => x.Definition!.Id)
                .ToDictionary(x => x.Key, x => x.Sum(y => y.Item.Quantity));
            var globalLowStocks = definitions
                .Where(x => x.GlobalSafetyStock > 0)
                .Select(x => new GlobalLowStockItem
                {
                    DefinitionId = x.Id,
                    Category = x.Category,
                    ItemName = x.ItemName,
                    Unit = x.Unit,
                    StoredQuantity = storedTotals.GetValueOrDefault(x.Id),
                    SafetyStock = x.GlobalSafetyStock
                })
                .Where(x => x.TotalQuantity <= x.SafetyStock)
                .OrderByDescending(x => x.Shortage)
                .ThenBy(x => x.ItemName)
                .ToList();

            return new DashboardViewModel
            {
                TotalLocationCount = locations.Count,
                TotalItemTypeCount = definitions.Count,
                TotalQuantity = items.Sum(x => x.Quantity),
                LowStockCount = locationLowStocks.Count,
                GlobalLowStockCount = globalLowStocks.Count,
                ExpiringSoonCount = items.Count(x => x.ExpirationDate >= today && x.ExpirationDate <= expiringDate),
                ExpiredCount = items.Count(x => x.ExpirationDate < today),
                LocationLowStockItems = locationLowStocks,
                GlobalLowStockItems = globalLowStocks,
                ExpiringSoonItems = items.Where(x => x.ExpirationDate >= today && x.ExpirationDate <= expiringDate).ToList(),
                LocationSummaries = locations.Select(location => new LocationSummaryItem
                {
                    LocationId = location.Id,
                    LocationName = location.LocationName,
                    ItemTypeCount = resolvedItems.Where(x => x.Item.LocationId == location.Id)
                        .Select(x => x.Definition!.Id).Distinct().Count(),
                    TotalQuantity = items.Where(x => x.LocationId == location.Id).Sum(x => x.Quantity),
                    LowStockCount = locationLowStocks.Count(x => x.LocationId == location.Id)
                }).ToList()
            };
        }
    }
}

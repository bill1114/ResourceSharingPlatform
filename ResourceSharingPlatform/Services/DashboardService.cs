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

            var items = await _context.SupplyItems
                .Include(x => x.Location)
                .Where(x => x.IsActive)
                .ToListAsync();

            var locations = await _context.SupplyLocations
                .Where(x => x.IsActive)
                .ToListAsync();

            return new DashboardViewModel
            {
                TotalLocationCount = locations.Count,
                TotalItemTypeCount = items.Count,
                TotalQuantity = items.Sum(x => x.Quantity),
                LowStockCount = items.Count(x => x.Quantity <= x.SafetyStock),
                ExpiringSoonCount = items.Count(x => x.ExpirationDate != null && x.ExpirationDate >= today && x.ExpirationDate <= expiringDate),
                ExpiredCount = items.Count(x => x.ExpirationDate != null && x.ExpirationDate < today),
                LowStockItems = items.Where(x => x.Quantity <= x.SafetyStock).ToList(),
                ExpiringSoonItems = items.Where(x => x.ExpirationDate != null && x.ExpirationDate >= today && x.ExpirationDate <= expiringDate).ToList(),
                LocationSummaries = locations.Select(l => new LocationSummaryItem
                {
                    LocationId = l.Id,
                    LocationName = l.LocationName,
                    ItemTypeCount = items.Count(i => i.LocationId == l.Id),
                    TotalQuantity = items.Where(i => i.LocationId == l.Id).Sum(i => i.Quantity),
                    LowStockCount = items.Count(i => i.LocationId == l.Id && i.Quantity <= i.SafetyStock)
                }).ToList()
            };
        }
    }
}

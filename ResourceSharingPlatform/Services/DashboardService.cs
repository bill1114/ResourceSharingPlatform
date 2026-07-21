using ResourceSharingPlatform.Models.ViewModels;
using ResourceSharingPlatform.Services.GoogleSheets;

namespace ResourceSharingPlatform.Services
{
    public class DashboardService
    {
        private readonly SheetsDataStore _store;

        public DashboardService(SheetsDataStore store)
        {
            _store = store;
        }

        public async Task<DashboardViewModel> GetDashboardAsync()
        {
            var today = DateTime.Today;
            var expiringDate = today.AddDays(30);

            var items = (await _store.GetItemsAsync()).Where(x => x.IsActive).ToList();
            var locations = (await _store.GetLocationsAsync()).Where(x => x.IsActive).ToList();

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

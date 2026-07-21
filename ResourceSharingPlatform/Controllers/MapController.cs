using Microsoft.AspNetCore.Mvc;
using ResourceSharingPlatform.Models.ViewModels;
using ResourceSharingPlatform.Services.GoogleSheets;

namespace ResourceSharingPlatform.Controllers
{
    public class MapController : Controller
    {
        private readonly SheetsDataStore _store;

        public MapController(SheetsDataStore store)
        {
            _store = store;
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

            var locations = (await _store.GetLocationsAsync())
                .Where(x => x.IsActive && x.Latitude != null && x.Longitude != null)
                .ToList();

            var items = (await _store.GetItemsAsync())
                .Where(x => x.IsActive)
                .ToList();

            var result = locations.Select(l => new MapLocationViewModel
            {
                LocationId = l.Id,
                LocationName = l.LocationName,
                Address = l.Address,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                ItemTypeCount = items.Count(i => i.LocationId == l.Id),
                TotalQuantity = items.Where(i => i.LocationId == l.Id).Sum(i => i.Quantity),
                LowStockCount = items.Count(i => i.LocationId == l.Id && i.Quantity <= i.SafetyStock),
                ExpiringSoonCount = items.Count(i => i.LocationId == l.Id && i.ExpirationDate != null && i.ExpirationDate >= today && i.ExpirationDate <= expiringDate)
            }).ToList();

            return Json(result);
        }
    }
}

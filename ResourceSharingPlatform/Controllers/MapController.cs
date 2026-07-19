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

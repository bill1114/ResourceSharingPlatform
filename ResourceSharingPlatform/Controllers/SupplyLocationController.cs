using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResourceSharingPlatform.Models;
using ResourceSharingPlatform.Services.GoogleSheets;

namespace ResourceSharingPlatform.Controllers
{
    public class SupplyLocationController : Controller
    {
        private readonly SheetsDataStore _store;

        public SupplyLocationController(SheetsDataStore store)
        {
            _store = store;
        }

        // GET: SupplyLocation
        public async Task<IActionResult> Index()
        {
            var locations = (await _store.GetLocationsAsync())
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .ToList();
            return View(locations);
        }

        // GET: SupplyLocation/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var location = await _store.GetLocationByIdAsync(id.Value);

            if (location == null)
            {
                return NotFound();
            }

            location.SupplyItems = (await _store.GetItemsAsync())
                .Where(x => x.LocationId == location.Id)
                .ToList();

            return View(location);
        }

        // GET: SupplyLocation/Create
        [Authorize(Roles = Roles.AdminAndCadre)]
        public IActionResult Create()
        {
            return View();
        }

        // POST: SupplyLocation/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdminAndCadre)]
        public async Task<IActionResult> Create([Bind("LocationName,Address,Latitude,Longitude,ContactPerson,Phone")] SupplyLocation location)
        {
            if (ModelState.IsValid)
            {
                location.IsActive = true;
                location.CreatedAt = DateTime.Now;
                await _store.CreateLocationAsync(location);
                TempData["SuccessMessage"] = "據點新增成功！";
                return RedirectToAction(nameof(Index));
            }
            return View(location);
        }

        // GET: SupplyLocation/Edit/5
        [Authorize(Roles = Roles.AdminAndCadre)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var location = await _store.GetLocationByIdAsync(id.Value);
            if (location == null)
            {
                return NotFound();
            }
            return View(location);
        }

        // POST: SupplyLocation/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdminAndCadre)]
        public async Task<IActionResult> Edit(int id, [Bind("Id,LocationName,Address,Latitude,Longitude,ContactPerson,Phone,IsActive,CreatedAt")] SupplyLocation location)
        {
            if (id != location.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                location.UpdatedAt = DateTime.Now;
                await _store.UpdateLocationAsync(location);
                TempData["SuccessMessage"] = "據點更新成功！";
                return RedirectToAction(nameof(Index));
            }
            return View(location);
        }

        // GET: SupplyLocation/Delete/5
        [Authorize(Roles = Roles.AdminAndCadre)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var location = await _store.GetLocationByIdAsync(id.Value);
            if (location == null)
            {
                return NotFound();
            }

            return View(location);
        }

        // POST: SupplyLocation/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdminAndCadre)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var location = await _store.GetLocationByIdAsync(id);
            if (location != null)
            {
                location.IsActive = false;
                location.UpdatedAt = DateTime.Now;
                await _store.UpdateLocationAsync(location);
                TempData["SuccessMessage"] = "據點已停用！";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

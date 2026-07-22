using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResourceSharingPlatform.Data;
using ResourceSharingPlatform.Models;

namespace ResourceSharingPlatform.Controllers
{
    public class SupplyLocationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SupplyLocationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: SupplyLocation
        public async Task<IActionResult> Index(string? keyword)
        {
            var query = _context.SupplyLocations.Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.LocationName.Contains(keyword) ||
                    (x.Address != null && x.Address.Contains(keyword)) ||
                    (x.ContactPerson != null && x.ContactPerson.Contains(keyword)) ||
                    (x.Phone != null && x.Phone.Contains(keyword)));
            }

            var locations = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();

            ViewBag.Keyword = keyword;

            return View(locations);
        }

        // GET: SupplyLocation/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var location = await _context.SupplyLocations
                .Include(x => x.SupplyItems)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (location == null)
            {
                return NotFound();
            }

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
                _context.Add(location);
                await _context.SaveChangesAsync();
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

            var location = await _context.SupplyLocations.FindAsync(id);
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
                try
                {
                    location.UpdatedAt = DateTime.Now;
                    _context.Update(location);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "據點更新成功！";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LocationExists(location.Id))
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

            var location = await _context.SupplyLocations
                .FirstOrDefaultAsync(m => m.Id == id);
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
            var location = await _context.SupplyLocations.FindAsync(id);
            if (location != null)
            {
                location.IsActive = false;
                location.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "據點已停用！";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool LocationExists(int id)
        {
            return _context.SupplyLocations.Any(e => e.Id == id);
        }
    }
}

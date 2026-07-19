using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ResourceSharingPlatform.Data;
using ResourceSharingPlatform.Models;
using ResourceSharingPlatform.Models.ViewModels;
using ResourceSharingPlatform.Services;

namespace ResourceSharingPlatform.Controllers
{
    public class SupplyOutboundController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SupplyOutboundService _outboundService;

        public SupplyOutboundController(ApplicationDbContext context, SupplyOutboundService outboundService)
        {
            _context = context;
            _outboundService = outboundService;
        }

        // GET: SupplyOutbound
        public async Task<IActionResult> Index()
        {
            var logs = await _context.SupplyOutboundLogs
                .Include(x => x.SupplyItem)
                .Include(x => x.Location)
                .OrderByDescending(x => x.OutboundTime)
                .Take(100)
                .ToListAsync();

            ViewBag.ExpiringItems = await GetExpiringItemsAsync();

            return View(logs);
        }

        // GET: SupplyOutbound/Create
        public async Task<IActionResult> Create(int? supplyItemId)
        {
            await PopulateItemsAsync(supplyItemId);
            ViewBag.ExpiringItems = await GetExpiringItemsAsync();
            return View(new OutboundViewModel { SupplyItemId = supplyItemId ?? 0 });
        }

        // POST: SupplyOutbound/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OutboundViewModel model)
        {
            if (ModelState.IsValid)
            {
                var operatorName = User.FindFirstValue("DisplayName") ?? User.Identity?.Name;
                var result = await _outboundService.IssueAsync(model, operatorName);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction(nameof(Index));
                }

                TempData["ErrorMessage"] = result.Message;
            }

            await PopulateItemsAsync(model.SupplyItemId);
            ViewBag.ExpiringItems = await GetExpiringItemsAsync();
            return View(model);
        }

        private async Task PopulateItemsAsync(int? selectedId)
        {
            var items = await _context.SupplyItems
                .Where(x => x.IsActive)
                .Include(x => x.Location)
                .ToListAsync();

            var today = DateTime.Today;

            var ordered = items
                .OrderBy(x => x.ExpirationDate.HasValue ? 0 : 1)
                .ThenBy(x => x.ExpirationDate)
                .ThenBy(x => x.ItemName)
                .Select(x => new
                {
                    x.Id,
                    DisplayText = x.ItemName + " (" + (x.Location?.LocationName ?? "") + ") - 現有 " + x.Quantity + " " + x.Unit
                        + (x.ExpirationDate.HasValue && x.ExpirationDate.Value < today ? " ⚠已過期" : "")
                        + (x.ExpirationDate.HasValue && x.ExpirationDate.Value >= today && x.ExpirationDate.Value <= today.AddDays(30) ? " ⚠即將過期" : "")
                })
                .ToList();

            ViewBag.Items = new SelectList(ordered, "Id", "DisplayText", selectedId);
        }

        private async Task<List<SupplyItem>> GetExpiringItemsAsync()
        {
            var today = DateTime.Today;
            var expiringDate = today.AddDays(30);

            return await _context.SupplyItems
                .Include(x => x.Location)
                .Where(x => x.IsActive && x.Quantity > 0 && x.ExpirationDate != null && x.ExpirationDate <= expiringDate)
                .OrderBy(x => x.ExpirationDate)
                .ToListAsync();
        }
    }
}

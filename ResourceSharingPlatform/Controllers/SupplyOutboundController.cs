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
        public async Task<IActionResult> Index(string? keyword)
        {
            var query = _context.SupplyOutboundLogs
                .Include(x => x.SupplyItem)
                .Include(x => x.Location)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    (x.SupplyItem != null && x.SupplyItem.ItemName.Contains(keyword)) ||
                    x.RecipientName.Contains(keyword) ||
                    (x.RecipientContact != null && x.RecipientContact.Contains(keyword)) ||
                    (x.Operator != null && x.Operator.Contains(keyword)) ||
                    (x.Remark != null && x.Remark.Contains(keyword)));
            }

            var logs = await query
                .OrderByDescending(x => x.OutboundTime)
                .Take(100)
                .ToListAsync();

            ViewBag.Keyword = keyword;
            ViewBag.ExpiringItems = await GetExpiringItemsAsync(GetMyLocationIdIfRestricted());

            return View(logs);
        }

        // GET: SupplyOutbound/RecipientAnalysis
        public async Task<IActionResult> RecipientAnalysis(string? keyword)
        {
            const int FrequentThreshold = 3;

            var query = _context.SupplyOutboundLogs.Include(x => x.SupplyItem).AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x => x.RecipientName.Contains(keyword));
            }

            var logs = await query.ToListAsync();

            var summary = logs
                .GroupBy(x => (x.RecipientName, Contact: x.RecipientContact ?? string.Empty))
                .Select(g =>
                {
                    var itemBreakdown = string.Join("、", g
                        .GroupBy(x => new { x.SupplyItem?.ItemName, x.SupplyItem?.Unit })
                        .Select(ig => $"{ig.Key.ItemName}（{ig.Count()}次，共{ig.Sum(x => x.OutboundQuantity)}{ig.Key.Unit}）"));

                    return new RecipientSummaryViewModel
                    {
                        RecipientName = g.Key.RecipientName,
                        RecipientContact = string.IsNullOrEmpty(g.Key.Contact) ? null : g.Key.Contact,
                        PickupCount = g.Count(),
                        IsFrequent = g.Count() >= FrequentThreshold,
                        ItemBreakdown = itemBreakdown,
                        FirstPickupDate = g.Min(x => x.OutboundTime),
                        LastPickupDate = g.Max(x => x.OutboundTime)
                    };
                })
                .OrderByDescending(x => x.PickupCount)
                .ToList();

            ViewBag.Keyword = keyword;

            return View(summary);
        }

        // GET: SupplyOutbound/Create
        public async Task<IActionResult> Create(int? supplyItemId, int? locationId)
        {
            var myLocationId = GetMyLocationIdIfRestricted();

            if (myLocationId == NoAccessSentinel)
            {
                TempData["ErrorMessage"] = "您的帳號尚未指定所屬據點，無法執行出庫，請聯絡管理人員設定。";
                return RedirectToAction(nameof(Index));
            }

            var model = new OutboundViewModel { SupplyItemId = supplyItemId ?? 0 };

            if (myLocationId.HasValue)
            {
                model.LocationId = myLocationId.Value;
            }
            else if (locationId.HasValue)
            {
                model.LocationId = locationId.Value;
            }

            await PopulateFormDataAsync(myLocationId);
            ViewBag.ExpiringItems = await GetExpiringItemsAsync(myLocationId);

            return View(model);
        }

        // POST: SupplyOutbound/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OutboundViewModel model)
        {
            var myLocationId = GetMyLocationIdIfRestricted();

            if (myLocationId == NoAccessSentinel)
            {
                TempData["ErrorMessage"] = "您的帳號尚未指定所屬據點，無法執行出庫，請聯絡管理人員設定。";
                return RedirectToAction(nameof(Index));
            }

            if (myLocationId.HasValue && model.LocationId != myLocationId.Value)
            {
                ModelState.AddModelError(string.Empty, "您沒有權限在此據點執行出庫");
            }

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

            await PopulateFormDataAsync(myLocationId);
            ViewBag.ExpiringItems = await GetExpiringItemsAsync(myLocationId);
            return View(model);
        }

        // Sentinel value distinguishing "no location assigned" (blocked) from "unrestricted" (null/Admin)
        private const int NoAccessSentinel = -1;

        private int? GetMyLocationIdIfRestricted()
        {
            if (User.IsInRole(Roles.Admin))
            {
                return null;
            }

            var locationClaim = User.FindFirstValue("LocationId");
            if (int.TryParse(locationClaim, out var locationId))
            {
                return locationId;
            }

            return NoAccessSentinel;
        }

        private async Task PopulateFormDataAsync(int? lockedLocationId)
        {
            ViewBag.Locations = new SelectList(
                await _context.SupplyLocations.Where(x => x.IsActive).ToListAsync(),
                "Id",
                "LocationName"
            );
            ViewBag.LockedLocationId = lockedLocationId;

            var items = await _context.SupplyItems
                .Where(x => x.IsActive)
                .Include(x => x.Location)
                .ToListAsync();

            var today = DateTime.Today;

            var ordered = items
                .OrderBy(x => x.ExpirationDate.HasValue ? 0 : 1)
                .ThenBy(x => x.ExpirationDate)
                .ThenBy(x => x.ItemName)
                .ToList();

            ViewBag.Items = ordered;
        }

        private async Task<List<SupplyItem>> GetExpiringItemsAsync(int? restrictToLocationId)
        {
            var today = DateTime.Today;
            var expiringDate = today.AddDays(30);

            var query = _context.SupplyItems
                .Include(x => x.Location)
                .Where(x => x.IsActive && x.Quantity > 0 && x.ExpirationDate != null && x.ExpirationDate <= expiringDate);

            if (restrictToLocationId.HasValue)
            {
                // NoAccessSentinel (-1) naturally matches no location, correctly showing nothing
                // for users who have no assigned location.
                query = query.Where(x => x.LocationId == restrictToLocationId.Value);
            }

            return await query.OrderBy(x => x.ExpirationDate).ToListAsync();
        }
    }
}

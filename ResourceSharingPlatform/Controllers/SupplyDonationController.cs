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
    public class SupplyDonationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SupplyDonationService _donationService;

        public SupplyDonationController(ApplicationDbContext context, SupplyDonationService donationService)
        {
            _context = context;
            _donationService = donationService;
        }

        // GET: SupplyDonation
        public async Task<IActionResult> Index(string? keyword)
        {
            var query = _context.SupplyDonationLogs
                .Include(x => x.SupplyItem)
                .Include(x => x.Location)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    (x.SupplyItem != null && x.SupplyItem.ItemName.Contains(keyword)) ||
                    x.DonorName.Contains(keyword) ||
                    (x.DonorContact != null && x.DonorContact.Contains(keyword)) ||
                    (x.Operator != null && x.Operator.Contains(keyword)) ||
                    (x.Remark != null && x.Remark.Contains(keyword)));
            }

            var logs = await query
                .OrderByDescending(x => x.DonationTime)
                .Take(100)
                .ToListAsync();

            ViewBag.Keyword = keyword;

            // 捐物者芳名錄：依捐贈者彙總（不受上面的關鍵字/100筆限制影響，抓全部紀錄統計）
            var allLogs = await _context.SupplyDonationLogs.ToListAsync();
            ViewBag.DonorSummary = allLogs
                .GroupBy(x => new { x.DonorName, x.DonorContact })
                .Select(g => new DonorSummaryViewModel
                {
                    DonorName = g.Key.DonorName,
                    DonorContact = g.Key.DonorContact,
                    DonationCount = g.Count(),
                    DistinctItemCount = g.Select(x => x.SupplyItemId).Distinct().Count(),
                    FirstDonationDate = g.Min(x => x.DonationTime),
                    LastDonationDate = g.Max(x => x.DonationTime)
                })
                .OrderByDescending(x => x.DonationCount)
                .ToList();

            return View(logs);
        }

        // GET: SupplyDonation/Create
        public async Task<IActionResult> Create(int? supplyItemId, int? locationId)
        {
            var myLocationId = GetMyLocationIdIfRestricted();

            if (myLocationId == NoAccessSentinel)
            {
                TempData["ErrorMessage"] = "您的帳號尚未指定所屬據點，無法登記捐贈，請聯絡管理人員設定。";
                return RedirectToAction(nameof(Index));
            }

            var model = new DonationViewModel { SupplyItemId = supplyItemId ?? 0 };

            if (myLocationId.HasValue)
            {
                model.LocationId = myLocationId.Value;
            }
            else if (locationId.HasValue)
            {
                model.LocationId = locationId.Value;
            }

            await PopulateFormDataAsync(myLocationId);

            return View(model);
        }

        // POST: SupplyDonation/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DonationViewModel model)
        {
            var myLocationId = GetMyLocationIdIfRestricted();

            if (myLocationId == NoAccessSentinel)
            {
                TempData["ErrorMessage"] = "您的帳號尚未指定所屬據點，無法登記捐贈，請聯絡管理人員設定。";
                return RedirectToAction(nameof(Index));
            }

            if (myLocationId.HasValue && model.LocationId != myLocationId.Value)
            {
                ModelState.AddModelError(string.Empty, "您沒有權限在此據點登記捐贈");
            }

            if (ModelState.IsValid)
            {
                var operatorName = User.FindFirstValue("DisplayName") ?? User.Identity?.Name;
                var result = await _donationService.DonateAsync(model, operatorName);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction(nameof(Index));
                }

                TempData["ErrorMessage"] = result.Message;
            }

            await PopulateFormDataAsync(myLocationId);
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
                .OrderBy(x => x.ItemName)
                .ToListAsync();

            ViewBag.Items = items;
        }
    }
}

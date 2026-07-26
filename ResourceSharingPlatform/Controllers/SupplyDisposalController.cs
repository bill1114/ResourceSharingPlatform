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
    public class SupplyDisposalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SupplyDisposalService _disposalService;

        public SupplyDisposalController(ApplicationDbContext context, SupplyDisposalService disposalService)
        {
            _context = context;
            _disposalService = disposalService;
        }

        // GET: SupplyDisposal
        public async Task<IActionResult> Index(string? keyword)
        {
            var logs = (await GetFilteredLogsAsync(keyword)).Take(100).ToList();

            ViewBag.Keyword = keyword;
            ViewBag.ExpiringItems = await GetExpiringItemsAsync(GetMyLocationIdIfRestricted());

            return View(logs);
        }

        // GET: SupplyDisposal/ExportExcel
        public async Task<IActionResult> ExportExcel(string? keyword)
        {
            var logs = await GetFilteredLogsAsync(keyword);

            var headers = new[] { "報廢時間", "物資名稱", "據點", "報廢數量", "報廢原因", "操作人員", "備註" };
            var rows = logs.Select(x => new object?[]
            {
                x.DisposalTime,
                x.SupplyItem?.ItemName,
                x.Location?.LocationName,
                x.DisposalQuantity,
                DisposalReasons.ToDisplayName(x.Reason),
                x.Operator,
                x.Remark
            });

            var bytes = ExcelExportHelper.BuildWorkbook("報廢明細", headers, rows);
            var fileName = $"報廢明細_{DateTime.Now:yyyyMMddHHmm}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private async Task<List<SupplyDisposalLog>> GetFilteredLogsAsync(string? keyword)
        {
            var query = _context.SupplyDisposalLogs
                .Include(x => x.SupplyItem)
                .Include(x => x.Location)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    (x.SupplyItem != null && x.SupplyItem.ItemName.Contains(keyword)) ||
                    (x.Operator != null && x.Operator.Contains(keyword)) ||
                    (x.Remark != null && x.Remark.Contains(keyword)));
            }

            return await query.OrderByDescending(x => x.DisposalTime).ToListAsync();
        }

        // GET: SupplyDisposal/Create
        public async Task<IActionResult> Create(int? supplyItemId, int? locationId)
        {
            var myLocationId = GetMyLocationIdIfRestricted();

            if (myLocationId == NoAccessSentinel)
            {
                TempData["ErrorMessage"] = "您的帳號尚未指定所屬據點，無法執行報廢，請聯絡管理人員設定。";
                return RedirectToAction(nameof(Index));
            }

            var model = new DisposalViewModel { SupplyItemId = supplyItemId ?? 0 };

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

        // POST: SupplyDisposal/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DisposalViewModel model)
        {
            var myLocationId = GetMyLocationIdIfRestricted();

            if (myLocationId == NoAccessSentinel)
            {
                TempData["ErrorMessage"] = "您的帳號尚未指定所屬據點，無法執行報廢，請聯絡管理人員設定。";
                return RedirectToAction(nameof(Index));
            }

            if (myLocationId.HasValue && model.LocationId != myLocationId.Value)
            {
                ModelState.AddModelError(string.Empty, "您沒有權限在此據點執行報廢");
            }

            if (ModelState.IsValid)
            {
                var operatorName = User.FindFirstValue("DisplayName") ?? User.Identity?.Name;
                var result = await _disposalService.DisposeAsync(model, operatorName);

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

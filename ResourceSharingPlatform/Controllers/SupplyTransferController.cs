using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ResourceSharingPlatform.Models;
using ResourceSharingPlatform.Models.ViewModels;
using ResourceSharingPlatform.Services;
using ResourceSharingPlatform.Services.GoogleSheets;

namespace ResourceSharingPlatform.Controllers
{
    public class SupplyTransferController : Controller
    {
        private readonly SheetsDataStore _store;
        private readonly SupplyTransferService _transferService;

        public SupplyTransferController(SheetsDataStore store, SupplyTransferService transferService)
        {
            _store = store;
            _transferService = transferService;
        }

        // GET: SupplyTransfer
        public async Task<IActionResult> Index()
        {
            var logs = (await _store.GetTransferLogsAsync())
                .OrderByDescending(x => x.TransferTime)
                .Take(100)
                .ToList();

            return View(logs);
        }

        // GET: SupplyTransfer/Create
        [Authorize(Roles = Roles.AdminAndCadre)]
        public async Task<IActionResult> Create(int? supplyItemId)
        {
            await PopulateDropdownsAsync();

            var model = new TransferBatchViewModel();
            if (supplyItemId.HasValue)
            {
                model.Lines[0].SupplyItemId = supplyItemId.Value;
            }

            return View(model);
        }

        // POST: SupplyTransfer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdminAndCadre)]
        public async Task<IActionResult> Create(TransferBatchViewModel model)
        {
            if (ModelState.IsValid)
            {
                var operatorName = string.IsNullOrWhiteSpace(model.Operator)
                    ? (User.FindFirstValue("DisplayName") ?? User.Identity?.Name)
                    : model.Operator;

                var result = await _transferService.CreateBatchAsync(model, operatorName);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction(nameof(Index));
                }

                TempData["ErrorMessage"] = result.Message;
            }

            await PopulateDropdownsAsync();
            return View(model);
        }

        // POST: SupplyTransfer/ConfirmReceipt/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ConfirmReceipt(int id)
        {
            var log = await _store.GetTransferLogByIdAsync(id);
            if (log == null)
            {
                TempData["ErrorMessage"] = "找不到轉移紀錄";
                return RedirectToAction(nameof(Index));
            }

            if (!CanResolveTransfer(log.ToLocationId))
            {
                TempData["ErrorMessage"] = "您沒有權限確認這筆轉移，僅目標據點人員或管理人員可操作";
                return RedirectToAction(nameof(Index));
            }

            var confirmedBy = User.FindFirstValue("DisplayName") ?? User.Identity?.Name;
            var result = await _transferService.ConfirmAsync(id, confirmedBy);

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // POST: SupplyTransfer/CancelTransfer/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CancelTransfer(int id)
        {
            var log = await _store.GetTransferLogByIdAsync(id);
            if (log == null)
            {
                TempData["ErrorMessage"] = "找不到轉移紀錄";
                return RedirectToAction(nameof(Index));
            }

            if (!CanResolveTransfer(log.ToLocationId))
            {
                TempData["ErrorMessage"] = "您沒有權限取消這筆轉移，僅目標據點人員或管理人員可操作";
                return RedirectToAction(nameof(Index));
            }

            var cancelledBy = User.FindFirstValue("DisplayName") ?? User.Identity?.Name;
            var result = await _transferService.CancelAsync(id, cancelledBy);

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        private bool CanResolveTransfer(int toLocationId)
        {
            if (User.IsInRole(Roles.Admin))
            {
                return true;
            }

            var locationClaim = User.FindFirstValue("LocationId");
            return int.TryParse(locationClaim, out var userLocationId) && userLocationId == toLocationId;
        }

        private async Task PopulateDropdownsAsync()
        {
            ViewBag.Items = (await _store.GetItemsAsync())
                .Where(x => x.IsActive)
                .OrderBy(x => x.ItemName)
                .ToList();

            ViewBag.Locations = new SelectList(
                (await _store.GetLocationsAsync()).Where(x => x.IsActive).ToList(),
                "Id",
                "LocationName"
            );
        }
    }
}

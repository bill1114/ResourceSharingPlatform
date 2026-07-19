using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ResourceSharingPlatform.Data;
using ResourceSharingPlatform.Models;
using ResourceSharingPlatform.Models.ViewModels;
using ResourceSharingPlatform.Services;

namespace ResourceSharingPlatform.Controllers
{
    public class SupplyTransferController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SupplyTransferService _transferService;

        public SupplyTransferController(ApplicationDbContext context, SupplyTransferService transferService)
        {
            _context = context;
            _transferService = transferService;
        }

        // GET: SupplyTransfer
        public async Task<IActionResult> Index()
        {
            var logs = await _context.SupplyTransferLogs
                .Include(x => x.SupplyItem)
                .Include(x => x.FromLocation)
                .Include(x => x.ToLocation)
                .OrderByDescending(x => x.TransferTime)
                .Take(100)
                .ToListAsync();

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
        [Authorize(Roles = Roles.AdminAndCadre)]
        public async Task<IActionResult> ConfirmReceipt(int id)
        {
            var confirmedBy = User.FindFirstValue("DisplayName") ?? User.Identity?.Name;
            var result = await _transferService.ConfirmAsync(id, confirmedBy);

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // POST: SupplyTransfer/CancelTransfer/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdminAndCadre)]
        public async Task<IActionResult> CancelTransfer(int id)
        {
            var cancelledBy = User.FindFirstValue("DisplayName") ?? User.Identity?.Name;
            var result = await _transferService.CancelAsync(id, cancelledBy);

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync()
        {
            ViewBag.Items = await _context.SupplyItems
                .Where(x => x.IsActive)
                .Include(x => x.Location)
                .OrderBy(x => x.ItemName)
                .ToListAsync();

            ViewBag.Locations = new SelectList(
                await _context.SupplyLocations.Where(x => x.IsActive).ToListAsync(),
                "Id",
                "LocationName"
            );
        }
    }
}

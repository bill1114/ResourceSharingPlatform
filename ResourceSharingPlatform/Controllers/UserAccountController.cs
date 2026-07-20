using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ResourceSharingPlatform.Data;
using ResourceSharingPlatform.Models;
using ResourceSharingPlatform.Models.ViewModels;

namespace ResourceSharingPlatform.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class UserAccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<UserAccount> _hasher;

        public UserAccountController(ApplicationDbContext context, IPasswordHasher<UserAccount> hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        // GET: UserAccount
        public async Task<IActionResult> Index()
        {
            var users = await _context.UserAccounts
                .Include(x => x.Location)
                .OrderBy(x => x.UserName)
                .ToListAsync();
            return View(users);
        }

        // GET: UserAccount/Create
        public async Task<IActionResult> Create()
        {
            await PopulateLocationsAsync();
            return View(new UserAccountViewModel());
        }

        // POST: UserAccount/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserAccountViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(nameof(model.Password), "新增帳號時必須設定密碼");
            }

            if (await _context.UserAccounts.AnyAsync(x => x.UserName == model.UserName))
            {
                ModelState.AddModelError(nameof(model.UserName), "此帳號已存在");
            }

            if (ModelState.IsValid)
            {
                var user = new UserAccount
                {
                    UserName = model.UserName,
                    DisplayName = model.DisplayName,
                    RoleName = model.RoleName,
                    LocationId = model.LocationId,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.Now
                };
                user.PasswordHash = _hasher.HashPassword(user, model.Password!);

                _context.UserAccounts.Add(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "帳號新增成功！";
                return RedirectToAction(nameof(Index));
            }

            await PopulateLocationsAsync();
            return View(model);
        }

        // GET: UserAccount/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.UserAccounts.FindAsync(id);
            if (user == null) return NotFound();

            await PopulateLocationsAsync();
            return View(new UserAccountViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                DisplayName = user.DisplayName,
                RoleName = user.RoleName,
                LocationId = user.LocationId,
                IsActive = user.IsActive
            });
        }

        // POST: UserAccount/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserAccountViewModel model)
        {
            if (id != model.Id) return NotFound();

            var user = await _context.UserAccounts.FindAsync(id);
            if (user == null) return NotFound();

            if (await _context.UserAccounts.AnyAsync(x => x.UserName == model.UserName && x.Id != id))
            {
                ModelState.AddModelError(nameof(model.UserName), "此帳號已存在");
            }

            if (!model.IsActive && user.RoleName == Roles.Admin)
            {
                var otherActiveAdmins = await _context.UserAccounts
                    .AnyAsync(x => x.RoleName == Roles.Admin && x.IsActive && x.Id != id);
                if (!otherActiveAdmins)
                {
                    ModelState.AddModelError(string.Empty, "至少需要保留一位啟用中的管理人員帳號");
                }
            }

            if (ModelState.IsValid)
            {
                user.UserName = model.UserName;
                user.DisplayName = model.DisplayName;
                user.RoleName = model.RoleName;
                user.LocationId = model.LocationId;
                user.IsActive = model.IsActive;
                user.UpdatedAt = DateTime.Now;

                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    user.PasswordHash = _hasher.HashPassword(user, model.Password);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "帳號更新成功！";
                return RedirectToAction(nameof(Index));
            }

            model.Id = id;
            await PopulateLocationsAsync();
            return View(model);
        }

        private async Task PopulateLocationsAsync()
        {
            ViewBag.Locations = new SelectList(
                await _context.SupplyLocations.Where(x => x.IsActive).ToListAsync(),
                "Id",
                "LocationName"
            );
        }

        // GET: UserAccount/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.UserAccounts.FirstOrDefaultAsync(x => x.Id == id);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: UserAccount/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.UserAccounts.FindAsync(id);
            if (user == null) return RedirectToAction(nameof(Index));

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == user.Id.ToString())
            {
                TempData["ErrorMessage"] = "無法停用您目前登入中的帳號";
                return RedirectToAction(nameof(Index));
            }

            if (user.RoleName == Roles.Admin)
            {
                var otherActiveAdmins = await _context.UserAccounts
                    .AnyAsync(x => x.RoleName == Roles.Admin && x.IsActive && x.Id != id);
                if (!otherActiveAdmins)
                {
                    TempData["ErrorMessage"] = "至少需要保留一位啟用中的管理人員帳號";
                    return RedirectToAction(nameof(Index));
                }
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "帳號已停用！";

            return RedirectToAction(nameof(Index));
        }
    }
}

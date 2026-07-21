using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ResourceSharingPlatform.Models;
using ResourceSharingPlatform.Models.ViewModels;
using ResourceSharingPlatform.Services.GoogleSheets;

namespace ResourceSharingPlatform.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly SheetsDataStore _store;
        private readonly IPasswordHasher<UserAccount> _hasher;

        public AccountController(SheetsDataStore store, IPasswordHasher<UserAccount> hasher)
        {
            _store = store;
            _hasher = hasher;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var candidate = await _store.GetUserByUsernameAsync(model.UserName);
            var user = candidate != null && candidate.IsActive ? candidate : null;

            var passwordOk = user != null &&
                _hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password) != PasswordVerificationResult.Failed;

            if (!passwordOk || user == null)
            {
                ModelState.AddModelError(string.Empty, "帳號或密碼錯誤");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.RoleName),
                new Claim("DisplayName", user.DisplayName ?? user.UserName)
            };

            if (user.LocationId.HasValue)
            {
                claims.Add(new Claim("LocationId", user.LocationId.Value.ToString()));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}

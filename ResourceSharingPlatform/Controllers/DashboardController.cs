using Microsoft.AspNetCore.Mvc;
using ResourceSharingPlatform.Services;

namespace ResourceSharingPlatform.Controllers
{
    public class DashboardController : Controller
    {
        private readonly DashboardService _dashboardService;

        public DashboardController(DashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _dashboardService.GetDashboardAsync();
            return View(model);
        }
    }
}

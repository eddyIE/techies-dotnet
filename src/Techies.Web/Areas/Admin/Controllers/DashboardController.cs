using Microsoft.AspNetCore.Mvc;
using Techies.Web.Infrastructure;
using Techies.Web.Services;

namespace Techies.Web.Areas.Admin.Controllers;

public sealed class DashboardController(DashboardQueries dashboard, TimeProvider clock) : AdminController
{
    // /admin is everyone's entry point; only Super Admin sees the dashboard (it shows revenue),
    // other roles are sent to their first allowed menu.
    public async Task<IActionResult> Index(RevenuePeriod period = RevenuePeriod.Last7Days)
    {
        if (!User.Allows(Policies.SuperAdmin))
        {
            var first = AdminMenu.For(User).FirstOrDefault();
            return first is null ? View("NoMenu") : RedirectToAction("Index", first.Controller);
        }
        return View(await dashboard.RevenueAsync(period, clock.GetUtcNow()));
    }
}

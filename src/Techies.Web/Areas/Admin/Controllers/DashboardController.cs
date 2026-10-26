using Microsoft.AspNetCore.Mvc;
using Techies.Web.Infrastructure;
using Techies.Web.Services;

namespace Techies.Web.Areas.Admin.Controllers;

public sealed class DashboardController(DashboardQueries dashboard, CurrentStaff current, TimeProvider clock) : AdminController
{
    // /admin is everyone's entry point; staff without dashboard.view (it shows revenue) are
    // sent to their first allowed menu.
    public async Task<IActionResult> Index(RevenuePeriod period = RevenuePeriod.Last7Days, RevenuePeriod top = RevenuePeriod.ThisMonth)
    {
        var access = await current.AccessAsync();
        if (!access.Has(Permissions.DashboardView))
        {
            var first = AdminMenu.For(access).FirstOrDefault();
            return first is null ? View("NoMenu") : RedirectToAction("Index", first.Controller, new { area = "Admin" });
        }
        var now = clock.GetUtcNow();
        var (awaiting, confirmed) = await dashboard.PendingCountsAsync();
        return View(new DashboardModel(awaiting, confirmed, await dashboard.RevenueAsync(period, now),
            await dashboard.RecentOrdersAsync(), top, await dashboard.BestSellersAsync(top, now)));
    }
}

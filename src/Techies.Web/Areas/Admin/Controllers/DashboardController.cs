using Microsoft.AspNetCore.Mvc;
using Techies.Web.Infrastructure;
using Techies.Web.Services;

namespace Techies.Web.Areas.Admin.Controllers;

public sealed class DashboardController(DashboardQueries dashboard, TimeProvider clock) : AdminController
{
    // /admin is everyone's entry point; only Super Admin sees the dashboard (it shows revenue),
    // other roles are sent to their first allowed menu.
    public async Task<IActionResult> Index(RevenuePeriod period = RevenuePeriod.Last7Days, RevenuePeriod top = RevenuePeriod.ThisMonth)
    {
        if (!User.Allows(Policies.SuperAdmin))
        {
            var first = AdminMenu.For(User).FirstOrDefault();
            return first is null ? View("NoMenu") : RedirectToAction("Index", first.Controller);
        }
        var now = clock.GetUtcNow();
        var (awaiting, confirmed) = await dashboard.PendingCountsAsync();
        return View(new DashboardModel(awaiting, confirmed, await dashboard.RevenueAsync(period, now),
            await dashboard.RecentOrdersAsync(), top, await dashboard.BestSellersAsync(top, now)));
    }
}

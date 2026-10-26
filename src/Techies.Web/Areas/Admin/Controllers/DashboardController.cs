using Microsoft.AspNetCore.Mvc;
using Techies.Web.Infrastructure;

namespace Techies.Web.Areas.Admin.Controllers;

public sealed class DashboardController : AdminController
{
    // /admin is everyone's entry point; only Super Admin sees the dashboard (it shows revenue),
    // other roles are sent to their first allowed menu.
    public IActionResult Index()
    {
        if (!User.Allows(Policies.SuperAdmin))
        {
            var first = AdminMenu.For(User).FirstOrDefault();
            return first is null ? View("NoMenu") : RedirectToAction("Index", first.Controller);
        }
        return View();
    }
}

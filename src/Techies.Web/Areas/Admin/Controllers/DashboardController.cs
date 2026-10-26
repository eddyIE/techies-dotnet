using Microsoft.AspNetCore.Mvc;

namespace Techies.Web.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class DashboardController : Controller
{
    public IActionResult Index() => View();
}

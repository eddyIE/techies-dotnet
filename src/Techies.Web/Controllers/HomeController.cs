using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Services;
using Techies.Web.ViewModels;

namespace Techies.Web.Controllers;

public class HomeController(ShopQueries shop) : Controller
{
    public async Task<IActionResult> Index() => View(await shop.HomeAsync());

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

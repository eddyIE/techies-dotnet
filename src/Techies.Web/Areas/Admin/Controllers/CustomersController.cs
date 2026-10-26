using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Services;

namespace Techies.Web.Areas.Admin.Controllers;

[Authorize(Policy = Policies.Orders)]
public sealed class CustomersController(CustomerQueries customers, LoyaltyService loyalty) : AdminController
{
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        ViewData["Q"] = q;
        ViewData["Tiers"] = (await loyalty.TiersAsync()).ToDictionary(t => t.Tier, t => t.Name);
        return View(await customers.SearchAsync(q, page));
    }

    public async Task<IActionResult> Details(Guid id) => View(await customers.ProfileAsync(id));

    [HttpPost]
    public async Task<IActionResult> ToggleLock(Guid id)
    {
        var status = await customers.ToggleLockAsync(id);
        TempData["Success"] = status == AccountStatus.Locked ? "Đã khóa tài khoản khách hàng." : "Đã mở khóa tài khoản.";
        return RedirectToAction(nameof(Details), new { id });
    }
}

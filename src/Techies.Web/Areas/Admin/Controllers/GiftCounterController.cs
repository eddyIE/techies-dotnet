using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Infrastructure;
using Techies.Web.Services;

namespace Techies.Web.Areas.Admin.Controllers;

[Authorize(Policy = Policies.Orders)]
public sealed class GiftCounterController(GiftService gifts) : AdminController
{
    public async Task<IActionResult> Index(string? code)
    {
        ViewData["Code"] = code;
        return View(string.IsNullOrWhiteSpace(code) ? null : await gifts.FindRedemptionAsync(code));
    }

    [HttpPost]
    public Task<IActionResult> Claim(string code) =>
        TryAsync(() => gifts.ClaimAsync(code, StaffId), "Đã xác nhận giao quà cho khách.",
            RedirectToAction(nameof(Index), new { code }));
}

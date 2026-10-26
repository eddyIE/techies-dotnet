using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Infrastructure;
using Techies.Web.Services;
using Techies.Web.ViewModels.Loyalty;

namespace Techies.Web.Areas.Admin.Controllers;

[RequirePermission(Permissions.TiersManage)]
public sealed class TiersController(LoyaltyService loyalty) : AdminController
{
    public async Task<IActionResult> Index() => View((await loyalty.TiersAsync())
        .Select(t => new TierInput { Tier = t.Tier, Name = t.Name, ThresholdPoints = t.ThresholdPoints, DiscountPercent = t.DiscountPercent })
        .ToList());

    [HttpPost]
    public Task<IActionResult> Index(List<TierInput> tiers) =>
        TryAsync(() => loyalty.UpdateTiersAsync(tiers.Where(t => t.Tier > 0).ToList()),
            "Đã lưu chính sách hạng thành viên và cập nhật hạng cho khách hàng.", RedirectToAction(nameof(Index)));
}

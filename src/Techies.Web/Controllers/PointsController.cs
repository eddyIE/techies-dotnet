using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Services;

namespace Techies.Web.Controllers;

[Route("diem-thuong")]
[Authorize(Policy = Policies.Customer)]
public sealed class PointsController(RewardsService rewards) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await rewards.OverviewAsync(User.UserId()));

    [HttpPost("doi-qua/{giftId:guid}")]
    public async Task<IActionResult> Redeem(Guid giftId)
    {
        try
        {
            var redemption = await rewards.RedeemAsync(User.UserId(), giftId);
            TempData["Success"] = $"Đổi quà thành công! Mã nhận quà của bạn: {redemption.Code}. Hãy đưa mã này cho nhân viên tại cửa hàng.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}

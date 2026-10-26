using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Services;

namespace Techies.Web.Controllers;

[Route("don-hang")]
[Authorize(Policy = Policies.Customer)]
public sealed class MyOrdersController(OrderQueries queries, OrderService orders, ReviewService reviews) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1) => View(await queries.ForCustomerAsync(User.UserId(), page));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var order = await queries.ForCustomerDetailAsync(User.UserId(), id);
        if (order is null) return NotFound();
        ViewData["Reviews"] = await reviews.ForOrderAsync(id);
        return View(order);
    }

    [HttpPost("{id:guid}/danh-gia")]
    public async Task<IActionResult> Review(Guid id, Guid productId, short rating, string? comment)
    {
        try
        {
            await reviews.AddAsync(User.UserId(), id, productId, rating, comment);
            TempData["Success"] = "Cảm ơn bạn đã đánh giá sản phẩm!";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/huy")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            await orders.CancelByCustomerAsync(id, User.UserId());
            TempData["Success"] = "Đã hủy đơn hàng.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }
}

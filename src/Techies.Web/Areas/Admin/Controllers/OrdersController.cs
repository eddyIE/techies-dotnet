using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Infrastructure;
using Techies.Web.Services;
using Techies.Web.ViewModels.Orders;

namespace Techies.Web.Areas.Admin.Controllers;

[Authorize(Policy = Policies.Orders)]
public sealed class OrdersController(OrderQueries queries, OrderService orders) : AdminController
{
    public async Task<IActionResult> Index([FromQuery] OrderFilter filter)
    {
        ViewData["Filter"] = filter;
        ViewData["Counts"] = await queries.CountsByStatusAsync();
        return View(await queries.ListAsync(filter));
    }

    public async Task<IActionResult> Details(Guid id) => View(await queries.DetailAsync(id));

    public async Task<IActionResult> DeliveryNote(Guid id) => View(await queries.DetailAsync(id));

    [HttpPost]
    public Task<IActionResult> Complete(Guid id) =>
        TryAsync(() => orders.CompleteAsync(id), "Đã hoàn tất đơn hàng và cộng điểm thưởng cho khách.",
            RedirectToAction(nameof(Details), new { id }));

    [HttpPost]
    public Task<IActionResult> Cancel(Guid id) =>
        TryAsync(() => orders.CancelByStaffAsync(id, StaffId), "Đã hủy đơn hàng và hoàn hàng về kho.",
            RedirectToAction(nameof(Details), new { id }));
}

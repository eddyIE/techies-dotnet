using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Infrastructure;
using Techies.Web.Services;
using Techies.Web.ViewModels.Orders;

namespace Techies.Web.Areas.Admin.Controllers;

[RequirePermission(Permissions.OrdersView)]
public sealed class OrdersController(OrderQueries queries, OrderService orders) : AdminController
{
    public async Task<IActionResult> Index([FromQuery] OrderFilter filter)
    {
        ViewData["Filter"] = filter;
        ViewData["Counts"] = await queries.CountsByStatusAsync();
        return View(await queries.ListAsync(filter));
    }

    public async Task<IActionResult> Details(Guid id) => View(await queries.DetailAsync(id));

    [RequirePermission(Permissions.OrdersPrint)]
    public async Task<IActionResult> DeliveryNote(Guid id) => View(await queries.DetailAsync(id));

    [HttpPost, RequirePermission(Permissions.OrdersComplete)]
    public Task<IActionResult> Complete(Guid id) =>
        TryAsync(() => orders.CompleteAsync(id), "Đã hoàn tất đơn hàng và cộng điểm thưởng cho khách.",
            RedirectToAction(nameof(Details), new { id }));

    [HttpPost, RequirePermission(Permissions.OrdersCancel)]
    public Task<IActionResult> Cancel(Guid id) =>
        TryAsync(() => orders.CancelByStaffAsync(id, StaffId), "Đã hủy đơn hàng và hoàn hàng về kho.",
            RedirectToAction(nameof(Details), new { id }));
}

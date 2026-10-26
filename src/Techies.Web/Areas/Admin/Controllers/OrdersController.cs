using Microsoft.AspNetCore.Mvc;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.Services;
using Techies.Web.ViewModels.Orders;

namespace Techies.Web.Areas.Admin.Controllers;

[RequirePermission(Permissions.OrdersView)]
public sealed class OrdersController(OrderQueries queries, OrderService orders, CurrentStaff staff) : AdminController
{
    public async Task<IActionResult> Index()
    {
        var request = GridRequest.From(Request.Path, Request.QueryString, OrderQueries.GridSorts, "created", true);
        var (rows, total) = await queries.GridAsync(request);
        var counts = await queries.CountsByStatusAsync();
        var access = await staff.AccessAsync();
        var mass = new List<GridMassAction>();
        if (access.Has(Permissions.OrdersComplete)) mass.Add(new("Hoàn tất đơn hàng", Url.Action(nameof(MassComplete))!, "Hoàn tất (đã giao và thu đủ tiền)"));
        if (access.Has(Permissions.OrdersCancel)) mass.Add(new("Hủy đơn", Url.Action(nameof(MassCancel))!, "Hủy và hoàn hàng về kho"));
        if (access.Has(Permissions.OrdersPrint)) mass.Add(new("In phiếu giao hàng", Url.Action(nameof(PrintNotes))!, "In phiếu giao hàng", NewTab: true));
        return GridView(new GridModel
        {
            Id = "orders", Request = request, Total = total, RowsPartial = "_OrderRows",
            KeywordPlaceholder = "Mã đơn (ORD-100117) hoặc SĐT người nhận",
            Columns = [new("Mã đơn", "ref"), new("Người nhận"), new("Thời gian đặt", "created"), new("Tổng tiền", "total", "text-end"),
                new("Thanh toán"), new("Trạng thái")],
            Filters =
            [
                new("status", "Trạng thái", GridFilterType.Select,
                    Enum.GetValues<OrderStatus>().Select(s => new GridOption(s.ToString(), $"{Labels.For(s).Text} ({counts[s]})", Labels.For(s).Text)).ToList()),
                new("method", "Phương thức", GridFilterType.Select, Enum.GetValues<PaymentMethod>().Select(m => new GridOption(m.ToString(), Labels.Short(m))).ToList()),
                new("payment", "Thanh toán", GridFilterType.Select, Enum.GetValues<PaymentStatus>().Select(p => new GridOption(p.ToString(), Labels.For(p).Text)).ToList()),
                new("total", "Tổng tiền (VNĐ)", GridFilterType.NumberRange, Money: true),
                new("created", "Ngày đặt", GridFilterType.DateRange),
            ],
            MassActions = mass,
            Rows = rows.Select(o => (o, RowActions(o, access))).ToList(),
        });
    }

    private List<GridRowAction> RowActions(OrderRow o, StaffAccess access)
    {
        var actions = new List<GridRowAction> { new("Xem chi tiết", Url.Action(nameof(Details), new { id = o.Id })!) };
        if (o.Status == OrderStatus.Confirmed && access.Has(Permissions.OrdersComplete))
            actions.Add(new("Hoàn tất đơn hàng", Url.Action(nameof(MassComplete), new { ids = o.Id })!, Post: true,
                Confirm: $"Xác nhận {o.OrderRef} đã giao thành công và thu đủ tiền?"));
        if (o.Status == OrderStatus.Confirmed && access.Has(Permissions.OrdersCancel))
            actions.Add(new("Hủy đơn", Url.Action(nameof(MassCancel), new { ids = o.Id })!, Post: true,
                Confirm: $"Hủy {o.OrderRef}? Hàng sẽ được trả về kho.", Danger: true));
        if (access.Has(Permissions.OrdersPrint))
            actions.Add(new("In phiếu giao hàng", Url.Action(nameof(DeliveryNote), new { id = o.Id })!, NewTab: true));
        return actions;
    }

    public async Task<IActionResult> Details(Guid id) => View(await queries.DetailAsync(id));

    [RequirePermission(Permissions.OrdersPrint)]
    public async Task<IActionResult> DeliveryNote(Guid id) => View("DeliveryNotes", new List<Order> { await queries.DetailAsync(id) });

    [HttpPost, RequirePermission(Permissions.OrdersPrint)]
    public async Task<IActionResult> PrintNotes(List<Guid> ids) =>
        ids.Count == 0 ? BackTo(null) : View("DeliveryNotes", await queries.DetailsAsync(ids));

    [HttpPost, RequirePermission(Permissions.OrdersComplete)]
    public Task<IActionResult> MassComplete(List<Guid> ids, string? returnUrl) =>
        MassAsync(ids, id => orders.CompleteAsync(id), "hoàn tất", returnUrl);

    [HttpPost, RequirePermission(Permissions.OrdersCancel)]
    public Task<IActionResult> MassCancel(List<Guid> ids, string? returnUrl) =>
        MassAsync(ids, id => orders.CancelByStaffAsync(id, StaffId), "hủy", returnUrl);

    [HttpPost, RequirePermission(Permissions.OrdersComplete)]
    public Task<IActionResult> Complete(Guid id) =>
        TryAsync(() => orders.CompleteAsync(id), "Đã hoàn tất đơn hàng và cộng điểm thưởng cho khách.",
            RedirectToAction(nameof(Details), new { id }));

    [HttpPost, RequirePermission(Permissions.OrdersCancel)]
    public Task<IActionResult> Cancel(Guid id) =>
        TryAsync(() => orders.CancelByStaffAsync(id, StaffId), "Đã hủy đơn hàng và hoàn hàng về kho.",
            RedirectToAction(nameof(Details), new { id }));
}

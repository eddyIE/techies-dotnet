using Microsoft.AspNetCore.Mvc;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.Services;
using Techies.Web.ViewModels.Customers;

namespace Techies.Web.Areas.Admin.Controllers;

[RequirePermission(Permissions.CustomersView)]
public sealed class CustomersController(CustomerQueries customers, LoyaltyService loyalty, CurrentStaff staff) : AdminController
{
    public async Task<IActionResult> Index()
    {
        var request = GridRequest.From(Request.Path, Request.QueryString, CustomerQueries.GridSorts, "name", false);
        var (rows, total) = await customers.GridAsync(request);
        var tiers = await loyalty.TiersAsync();
        var canLock = await staff.HasAsync(Permissions.CustomersLock);
        ViewData["Tiers"] = tiers.ToDictionary(t => t.Tier, t => t.Name);
        return GridView(new GridModel
        {
            Id = "customers", Request = request, Total = total, RowsPartial = "_CustomerRows",
            KeywordPlaceholder = "Email, họ tên hoặc số điện thoại",
            Columns = [new("Khách hàng", "name"), new("Email", "email"), new("Điện thoại"), new("Hạng"),
                new("Điểm khả dụng", "balance", "text-end"), new("Trạng thái")],
            Filters =
            [
                new("tier", "Hạng thành viên", GridFilterType.Select, tiers.Select(t => new GridOption(t.Tier.ToString(), t.Name)).ToList()),
                new("status", "Trạng thái", GridFilterType.Select, [new("Active", "Hoạt động"), new("Locked", "Đã khóa")]),
            ],
            MassActions = canLock
                ? [new("Khóa tài khoản", Url.Action(nameof(MassLock))!, "Khóa tài khoản (khách bị đăng xuất ngay)"), new("Mở khóa", Url.Action(nameof(MassUnlock))!)]
                : [],
            Rows = rows.Select(c => (c, RowActions(c, canLock))).ToList(),
        });
    }

    private List<GridRowAction> RowActions(CustomerRow c, bool canLock)
    {
        var actions = new List<GridRowAction> { new("Xem hồ sơ", Url.Action(nameof(Details), new { id = c.Id })!) };
        if (canLock)
            actions.Add(c.Status == AccountStatus.Active
                ? new("Khóa tài khoản", Url.Action(nameof(MassLock), new { ids = c.Id })!, Post: true, Confirm: $"Khóa tài khoản {c.FullName}?", Danger: true)
                : new("Mở khóa", Url.Action(nameof(MassUnlock), new { ids = c.Id })!, Post: true));
        return actions;
    }

    public async Task<IActionResult> Details(Guid id) => View(await customers.ProfileAsync(id));

    [HttpPost, RequirePermission(Permissions.CustomersLock)]
    public Task<IActionResult> MassLock(List<Guid> ids, string? returnUrl) =>
        MassAsync(ids, id => customers.SetStatusAsync(id, AccountStatus.Locked), "khóa", returnUrl);

    [HttpPost, RequirePermission(Permissions.CustomersLock)]
    public Task<IActionResult> MassUnlock(List<Guid> ids, string? returnUrl) =>
        MassAsync(ids, id => customers.SetStatusAsync(id, AccountStatus.Active), "mở khóa", returnUrl);

    // Lock button on the profile page.
    [HttpPost, RequirePermission(Permissions.CustomersLock)]
    public async Task<IActionResult> ToggleLock(Guid id)
    {
        var profile = await customers.ProfileAsync(id);
        var locking = profile.User.Status == AccountStatus.Active;
        await customers.SetStatusAsync(id, locking ? AccountStatus.Locked : AccountStatus.Active);
        TempData["Success"] = locking ? "Đã khóa tài khoản khách hàng." : "Đã mở khóa tài khoản.";
        return RedirectToAction(nameof(Details), new { id });
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.Services;
using Techies.Web.ViewModels.Promotions;

namespace Techies.Web.Areas.Admin.Controllers;

[RequirePermission(Permissions.CouponsView)]
public sealed class CouponsController(CouponService coupons, CurrentStaff staff, TimeProvider clock) : AdminController
{
    public async Task<IActionResult> Index()
    {
        var request = GridRequest.From(Request.Path, Request.QueryString, CouponService.GridSorts, "code", false);
        var (rows, total) = await coupons.GridAsync(request);
        var canManage = await staff.HasAsync(Permissions.CouponsManage);
        var now = clock.GetUtcNow();
        ViewData["Now"] = now;
        return GridView(new GridModel
        {
            Id = "coupons", Request = request, Total = total, RowsPartial = "_CouponRows",
            KeywordPlaceholder = "Mã hoặc mô tả...",
            Columns = [new("Mã", "code"), new("Mô tả"), new("Giảm", "discount", "text-end"), new("Đơn tối thiểu", "min", "text-end"),
                new("Bắt đầu", "starts"), new("Hết hạn", "expires"), new("Trạng thái")],
            Filters =
            [
                new("status", "Trạng thái", GridFilterType.Select, Enum.GetValues<CouponStatus>().Select(s => new GridOption(s.ToString(), Labels.For(s).Text)).ToList()),
                new("discount", "Số tiền giảm (VNĐ)", GridFilterType.NumberRange, Money: true),
                new("expires", "Ngày hết hạn", GridFilterType.DateRange),
            ],
            MassActions = canManage ? [new("Tạm dừng", Url.Action(nameof(MassPause))!), new("Kích hoạt", Url.Action(nameof(MassActivate))!)] : [],
            Rows = rows.Select(c => (c, canManage
                ? new List<GridRowAction>
                {
                    new("Sửa", Url.Action(nameof(Edit), new { id = c.Id })!),
                    c.Active ? new("Tạm dừng", Url.Action(nameof(MassPause), new { ids = c.Id })!, Post: true)
                             : new("Kích hoạt", Url.Action(nameof(MassActivate), new { ids = c.Id })!, Post: true),
                }
                : [])).ToList(),
        });
    }

    [HttpPost, RequirePermission(Permissions.CouponsManage)]
    public Task<IActionResult> MassPause(List<Guid> ids, string? returnUrl) =>
        MassAsync(ids, id => coupons.SetActiveAsync(id, false), "tạm dừng", returnUrl);

    [HttpPost, RequirePermission(Permissions.CouponsManage)]
    public Task<IActionResult> MassActivate(List<Guid> ids, string? returnUrl) =>
        MassAsync(ids, id => coupons.SetActiveAsync(id, true), "kích hoạt", returnUrl);

    [HttpGet, RequirePermission(Permissions.CouponsManage)]
    public IActionResult Create() => FormView(new CouponForm(), null);

    [HttpPost, RequirePermission(Permissions.CouponsManage)]
    public async Task<IActionResult> Create(CouponForm form)
    {
        if (!ModelState.IsValid) return FormView(form, null);
        try
        {
            await coupons.CreateAsync(form, StaffId);
            TempData["Success"] = "Đã tạo mã giảm giá.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return FormView(form, null);
        }
    }

    [HttpGet, RequirePermission(Permissions.CouponsManage)]
    public async Task<IActionResult> Edit(Guid id)
    {
        var c = await coupons.GetAsync(id);
        return FormView(new CouponForm
        {
            Code = c.Code, Description = c.Description, DiscountAmount = c.DiscountAmount, MinSubtotal = c.MinSubtotal,
            StartsAt = Format.ToLocalInput(c.StartsAt), ExpiresAt = Format.ToLocalInput(c.ExpiresAt),
        }, id);
    }

    [HttpPost, RequirePermission(Permissions.CouponsManage)]
    public async Task<IActionResult> Edit(Guid id, CouponForm form)
    {
        if (!ModelState.IsValid) return FormView(form, id);
        try
        {
            await coupons.UpdateAsync(id, form);
            TempData["Success"] = "Đã lưu mã giảm giá.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return FormView(form, id);
        }
    }


    private ViewResult FormView(CouponForm form, Guid? id)
    {
        ViewData["Id"] = id;
        return View("Edit", form);
    }
}

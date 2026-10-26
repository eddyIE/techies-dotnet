using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.Services;
using Techies.Web.ViewModels.Loyalty;

namespace Techies.Web.Areas.Admin.Controllers;

[RequirePermission(Permissions.GiftsView)]
public sealed class GiftsController(GiftService gifts, LoyaltyService loyalty, ImageStorage images, CurrentStaff staff) : AdminController
{
    public async Task<IActionResult> Index()
    {
        var request = GridRequest.From(Request.Path, Request.QueryString, GiftService.GridSorts, "points", false);
        var (rows, total) = await gifts.GridAsync(request);
        var tiers = await loyalty.TiersAsync();
        var canManage = await staff.HasAsync(Permissions.GiftsManage);
        ViewData["Tiers"] = tiers.ToDictionary(t => t.Tier, t => t.Name);
        return GridView(new GridModel
        {
            Id = "gifts", Request = request, Total = total, RowsPartial = "_GiftRows",
            KeywordPlaceholder = "Tìm quà tặng...",
            Columns = [new(""), new("Quà tặng", "name"), new("Điểm", "points", "text-end"), new("Hạng tối thiểu", "tier"),
                new("Còn lại", "stock", "text-end"), new("Trạng thái")],
            Filters =
            [
                new("tier", "Hạng tối thiểu", GridFilterType.Select, tiers.Select(t => new GridOption(t.Tier.ToString(), t.Name)).ToList()),
                new("points", "Điểm cần đổi", GridFilterType.NumberRange),
                new("stock", "Số lượng còn", GridFilterType.NumberRange),
                new("active", "Trạng thái", GridFilterType.Select, [new("true", "Mở đổi"), new("false", "Tạm khóa")]),
            ],
            MassActions = canManage ? [new("Mở đổi quà", Url.Action(nameof(MassOpen))!), new("Tạm khóa", Url.Action(nameof(MassClose))!)] : [],
            Rows = rows.Select(g => (g, canManage
                ? new List<GridRowAction>
                {
                    new("Sửa", Url.Action(nameof(Edit), new { id = g.Id })!),
                    g.Active ? new("Tạm khóa", Url.Action(nameof(MassClose), new { ids = g.Id })!, Post: true)
                             : new("Mở đổi quà", Url.Action(nameof(MassOpen), new { ids = g.Id })!, Post: true),
                }
                : [])).ToList(),
        });
    }

    [HttpPost, RequirePermission(Permissions.GiftsManage)]
    public Task<IActionResult> MassOpen(List<Guid> ids, string? returnUrl) =>
        MassAsync(ids, id => gifts.SetActiveAsync(id, true), "mở đổi", returnUrl);

    [HttpPost, RequirePermission(Permissions.GiftsManage)]
    public Task<IActionResult> MassClose(List<Guid> ids, string? returnUrl) =>
        MassAsync(ids, id => gifts.SetActiveAsync(id, false), "tạm khóa", returnUrl);

    [HttpGet, RequirePermission(Permissions.GiftsManage)]
    public Task<IActionResult> Create() => FormView(new GiftForm(), null);

    [HttpPost, RequirePermission(Permissions.GiftsManage)]
    public async Task<IActionResult> Create(GiftForm form)
    {
        if (!ModelState.IsValid) return await FormView(form, null);
        try
        {
            await gifts.CreateAsync(form, form.Image is null ? null : await images.SaveAsync(form.Image));
            TempData["Success"] = "Đã thêm quà tặng.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return await FormView(form, null);
        }
    }

    [HttpGet, RequirePermission(Permissions.GiftsManage)]
    public async Task<IActionResult> Edit(Guid id)
    {
        var g = await gifts.GetAsync(id);
        return await FormView(new GiftForm
        {
            Name = g.Name, Description = g.Description, CurrentImageUrl = g.ImageUrl, PointsCost = g.PointsCost,
            MinTier = g.MinTier, StockAvailable = g.StockAvailable, Active = g.Active,
        }, id);
    }

    [HttpPost, RequirePermission(Permissions.GiftsManage)]
    public async Task<IActionResult> Edit(Guid id, GiftForm form)
    {
        if (!ModelState.IsValid) return await FormView(form, id);
        try
        {
            await gifts.UpdateAsync(id, form, form.Image is null ? null : await images.SaveAsync(form.Image));
            TempData["Success"] = "Đã lưu quà tặng.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return await FormView(form, id);
        }
    }

    private async Task<IActionResult> FormView(GiftForm form, Guid? id)
    {
        ViewData["Id"] = id;
        ViewData["Tiers"] = await loyalty.TiersAsync();
        return View("Edit", form);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Services;
using Techies.Web.ViewModels.Loyalty;

namespace Techies.Web.Areas.Admin.Controllers;

[RequirePermission(Permissions.GiftsView)]
public sealed class GiftsController(GiftService gifts, LoyaltyService loyalty, ImageStorage images) : AdminController
{
    public async Task<IActionResult> Index()
    {
        ViewData["Tiers"] = await loyalty.TiersAsync();
        return View(await gifts.ListAsync());
    }

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

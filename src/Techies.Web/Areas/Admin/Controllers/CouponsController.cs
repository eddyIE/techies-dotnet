using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Services;
using Techies.Web.ViewModels.Promotions;

namespace Techies.Web.Areas.Admin.Controllers;

[Authorize(Policy = Policies.SuperAdmin)]
public sealed class CouponsController(CouponService coupons, TimeProvider clock) : AdminController
{
    public async Task<IActionResult> Index()
    {
        ViewData["Now"] = clock.GetUtcNow();
        return View(await coupons.ListAsync());
    }

    [HttpGet]
    public IActionResult Create() => FormView(new CouponForm(), null);

    [HttpPost]
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

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var c = await coupons.GetAsync(id);
        return FormView(new CouponForm
        {
            Code = c.Code, Description = c.Description, DiscountAmount = c.DiscountAmount, MinSubtotal = c.MinSubtotal,
            StartsAt = Format.ToLocalInput(c.StartsAt), ExpiresAt = Format.ToLocalInput(c.ExpiresAt),
        }, id);
    }

    [HttpPost]
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

    [HttpPost]
    public async Task<IActionResult> Toggle(Guid id)
    {
        var active = await coupons.ToggleActiveAsync(id);
        TempData["Success"] = active ? "Đã kích hoạt lại mã." : "Đã tạm dừng mã.";
        return RedirectToAction(nameof(Index));
    }

    private ViewResult FormView(CouponForm form, Guid? id)
    {
        ViewData["Id"] = id;
        return View("Edit", form);
    }
}

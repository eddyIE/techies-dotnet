using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Services;
using Techies.Web.ViewModels.Staff;

namespace Techies.Web.Areas.Admin.Controllers;

[Authorize(Policy = Policies.SuperAdmin)]
public sealed class StaffController(StaffService staff) : AdminController
{
    public async Task<IActionResult> Index() => View(await staff.ListAsync());

    [HttpGet]
    public IActionResult Create() => FormView(new StaffForm(), null);

    [HttpPost]
    public async Task<IActionResult> Create(StaffForm form)
    {
        if (!ModelState.IsValid) return FormView(form, null);
        try
        {
            await staff.CreateAsync(form);
            TempData["Success"] = $"Đã tạo tài khoản cho {form.FullName}. Hãy gửi email và mật khẩu khởi tạo cho nhân viên.";
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
        var s = await staff.GetAsync(id);
        ViewData["Status"] = s.Status;
        return FormView(new StaffForm { FullName = s.FullName, Email = s.Email, Phone = s.Phone, Role = s.Role }, id);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, StaffForm form)
    {
        ModelState.Remove(nameof(form.Password));
        if (!ModelState.IsValid) return FormView(form, id);
        return await TryAsync(() => staff.UpdateAsync(id, form, StaffId), "Đã lưu thông tin nhân viên.",
            RedirectToAction(nameof(Edit), new { id }));
    }

    [HttpPost]
    public Task<IActionResult> ResetPassword(Guid id, string newPassword) =>
        TryAsync(() => staff.ResetPasswordAsync(id, newPassword), "Đã đặt lại mật khẩu.", RedirectToAction(nameof(Edit), new { id }));

    [HttpPost]
    public Task<IActionResult> ToggleLock(Guid id) =>
        TryAsync(() => staff.ToggleLockAsync(id, StaffId), "Đã cập nhật trạng thái tài khoản.", RedirectToAction(nameof(Index)));

    private ViewResult FormView(StaffForm form, Guid? id)
    {
        ViewData["Id"] = id;
        return View("Edit", form);
    }
}

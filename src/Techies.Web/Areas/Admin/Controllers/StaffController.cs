using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.Services;
using Techies.Web.ViewModels.Staff;

namespace Techies.Web.Areas.Admin.Controllers;

[Authorize(Policy = Policies.SuperAdmin)]
public sealed class StaffController(StaffService staff, RoleService roles) : AdminController
{
    public async Task<IActionResult> Index()
    {
        var request = GridRequest.From(Request.Path, Request.QueryString, StaffService.GridSorts, "name", false);
        var (rows, total) = await staff.GridAsync(request);
        return GridView(new GridModel
        {
            Id = "staff", Request = request, Total = total, RowsPartial = "_StaffRows",
            KeywordPlaceholder = "Tên, email hoặc số điện thoại",
            Columns = [new("Nhân viên", "name"), new("Email", "email"), new("Điện thoại"), new("Vai trò", "role"), new("Trạng thái")],
            Filters =
            [
                new("role", "Vai trò", GridFilterType.Select, (await roles.ListAsync()).Select(r => new GridOption(r.Id.ToString(), r.Name)).ToList()),
                new("status", "Trạng thái", GridFilterType.Select, [new("Active", "Đang hoạt động"), new("Locked", "Đã khóa")]),
            ],
            MassActions = [new("Khóa tài khoản", Url.Action(nameof(MassLock))!, "Khóa tài khoản (nhân viên bị đăng xuất ngay)"), new("Mở khóa", Url.Action(nameof(MassUnlock))!)],
            Rows = rows.Select(s => (s, RowActions(s))).ToList(),
        });
    }

    private List<GridRowAction> RowActions(Staff s)
    {
        var actions = new List<GridRowAction> { new("Sửa / cấp lại mật khẩu", Url.Action(nameof(Edit), new { id = s.Id })!) };
        if (s.Id != StaffId)
            actions.Add(s.Status == AccountStatus.Active
                ? new("Khóa tài khoản", Url.Action(nameof(MassLock), new { ids = s.Id })!, Post: true, Confirm: $"Khóa tài khoản {s.FullName}? Nhân viên sẽ bị đăng xuất ngay.", Danger: true)
                : new("Mở khóa", Url.Action(nameof(MassUnlock), new { ids = s.Id })!, Post: true));
        return actions;
    }

    [HttpPost]
    public Task<IActionResult> MassLock(List<Guid> ids, string? returnUrl) =>
        MassAsync(ids, id => staff.SetStatusAsync(id, AccountStatus.Locked, StaffId), "khóa", returnUrl);

    [HttpPost]
    public Task<IActionResult> MassUnlock(List<Guid> ids, string? returnUrl) =>
        MassAsync(ids, id => staff.SetStatusAsync(id, AccountStatus.Active, StaffId), "mở khóa", returnUrl);

    [HttpGet]
    public Task<IActionResult> Create() => FormView(new StaffForm(), null);

    [HttpPost]
    public async Task<IActionResult> Create(StaffForm form)
    {
        if (!ModelState.IsValid) return await FormView(form, null);
        try
        {
            await staff.CreateAsync(form);
            TempData["Success"] = $"Đã tạo tài khoản cho {form.FullName}. Hãy gửi email và mật khẩu khởi tạo cho nhân viên.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return await FormView(form, null);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var s = await staff.GetAsync(id);
        ViewData["Status"] = s.Status;
        return await FormView(new StaffForm { FullName = s.FullName, Email = s.Email, Phone = s.Phone, RoleId = s.RoleId }, id);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, StaffForm form)
    {
        ModelState.Remove(nameof(form.Password));
        if (!ModelState.IsValid) return await FormView(form, id);
        return await TryAsync(() => staff.UpdateAsync(id, form, StaffId), "Đã lưu thông tin nhân viên.",
            RedirectToAction(nameof(Edit), new { id }));
    }

    [HttpPost]
    public Task<IActionResult> ResetPassword(Guid id, string newPassword) =>
        TryAsync(() => staff.ResetPasswordAsync(id, newPassword), "Đã đặt lại mật khẩu.", RedirectToAction(nameof(Edit), new { id }));


    private async Task<IActionResult> FormView(StaffForm form, Guid? id)
    {
        ViewData["Id"] = id;
        ViewData["Roles"] = await roles.ListAsync();
        return View("Edit", form);
    }
}

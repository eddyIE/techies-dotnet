using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.Services;
using Techies.Web.ViewModels.Staff;

namespace Techies.Web.Areas.Admin.Controllers;

[Authorize(Policy = Policies.SuperAdmin)]
public sealed class RolesController(RoleService roles) : AdminController
{
    public async Task<IActionResult> Index()
    {
        var request = GridRequest.From(Request.Path, Request.QueryString, RoleService.GridSorts, "id", false);
        var (rows, total) = await roles.GridAsync(request);
        return GridView(new GridModel
        {
            Id = "roles", Request = request, Total = total, RowsPartial = "_RoleRows",
            KeywordPlaceholder = "Tìm vai trò...",
            Columns = [new("Vai trò", "name"), new("Mô tả"), new("Nhân viên", "staff", "text-end"), new("Quyền", "permissions", "text-end")],
            MassActions = [new("Xóa vai trò", Url.Action(nameof(MassDelete))!, "Xóa vai trò (vai trò đang được gán sẽ được bỏ qua)")],
            Rows = rows.Select(r => (r, r.IsSystem
                ? new List<GridRowAction> { new("Xem", Url.Action(nameof(Edit), new { id = r.Id })!) }
                : new List<GridRowAction>
                {
                    new("Sửa quyền", Url.Action(nameof(Edit), new { id = r.Id })!),
                    new("Xóa", Url.Action(nameof(MassDelete), new { ids = r.Id })!, Post: true, Confirm: $"Xóa vai trò {r.Name}?", Danger: true),
                })).ToList(),
        });
    }

    [HttpPost]
    public Task<IActionResult> MassDelete(List<int> ids, string? returnUrl) =>
        MassAsync(ids, id => roles.DeleteAsync(id), "xóa", returnUrl);

    [HttpGet]
    public IActionResult Create() => FormView(new RoleForm(), null, isSystem: false);

    [HttpPost]
    public async Task<IActionResult> Create(RoleForm form)
    {
        if (!ModelState.IsValid) return FormView(form, null, false);
        try
        {
            await roles.CreateAsync(form);
            TempData["Success"] = $"Đã tạo vai trò “{form.Name}”.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return FormView(form, null, false);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var role = await roles.GetAsync(id);
        var codes = role.IsSystem
            ? Permissions.All.Select(p => p.Code).ToList()
            : role.Permissions.Select(rp => Permissions.All.Single(p => p.Id == rp.PermissionId).Code).ToList();
        return FormView(new RoleForm { Name = role.Name, Description = role.Description, Permissions = codes }, id, role.IsSystem);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, RoleForm form)
    {
        if (!ModelState.IsValid) return FormView(form, id, false);
        try
        {
            await roles.UpdateAsync(id, form);
            TempData["Success"] = "Đã lưu vai trò. Nhân viên có vai trò này nhận quyền mới ở thao tác tiếp theo.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return FormView(form, id, false);
        }
    }

    [HttpPost]
    public Task<IActionResult> Delete(int id) =>
        TryAsync(() => roles.DeleteAsync(id), "Đã xóa vai trò.", RedirectToAction(nameof(Index)));

    private ViewResult FormView(RoleForm form, int? id, bool isSystem)
    {
        ViewData["Id"] = id;
        ViewData["IsSystem"] = isSystem;
        return View("Edit", form);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Services;
using Techies.Web.ViewModels.Staff;

namespace Techies.Web.Areas.Admin.Controllers;

[Authorize(Policy = Policies.SuperAdmin)]
public sealed class RolesController(RoleService roles) : AdminController
{
    public async Task<IActionResult> Index() => View(await roles.RowsAsync());

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

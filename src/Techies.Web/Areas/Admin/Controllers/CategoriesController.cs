using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Services;
using Techies.Web.ViewModels.Catalog;

namespace Techies.Web.Areas.Admin.Controllers;

[RequirePermission(Permissions.CategoriesManage)]
public sealed class CategoriesController(CategoryService categories, ImageStorage images) : AdminController
{
    public async Task<IActionResult> Index() => View(await categories.ListAsync());

    [HttpGet]
    public IActionResult Create() => View("Edit", new CategoryForm());

    [HttpPost]
    public async Task<IActionResult> Create(CategoryForm form)
    {
        if (!ModelState.IsValid) return View("Edit", form);
        try
        {
            var iconUrl = form.Icon is null ? null : await images.SaveAsync(form.Icon);
            await categories.CreateAsync(form, iconUrl);
            TempData["Success"] = "Đã tạo danh mục.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View("Edit", form);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var category = await categories.GetAsync(id);
        ViewData["Id"] = id;
        return View(new CategoryForm
        {
            Name = category.Name, DisplayOrder = category.DisplayOrder, CurrentImageUrl = category.ImageUrl,
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, CategoryForm form)
    {
        ViewData["Id"] = id;
        if (!ModelState.IsValid) return View(form);
        try
        {
            var iconUrl = form.Icon is null ? null : await images.SaveAsync(form.Icon);
            await categories.UpdateAsync(id, form, iconUrl);
            TempData["Success"] = "Đã cập nhật danh mục.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(form);
        }
    }
}

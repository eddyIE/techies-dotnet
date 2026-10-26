using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.Services;
using Techies.Web.ViewModels.Catalog;

namespace Techies.Web.Areas.Admin.Controllers;

[RequirePermission(Permissions.CategoriesManage)]
public sealed class CategoriesController(CategoryService categories, ImageStorage images, CurrentStaff staff) : AdminController
{
    public async Task<IActionResult> Index()
    {
        var request = GridRequest.From(Request.Path, Request.QueryString, CategoryService.GridSorts, "order", false);
        var (rows, total) = await categories.GridAsync(request);
        var canSeeProducts = await staff.HasAsync(Permissions.ProductsView);
        return GridView(new GridModel
        {
            Id = "categories", Request = request, Total = total, RowsPartial = "_CategoryRows",
            KeywordPlaceholder = "Tìm danh mục...",
            Columns = [new(""), new("Tên danh mục", "name"), new("Slug"), new("Sản phẩm", "products", "text-end"), new("Thứ tự", "order", "text-end")],
            Rows = rows.Select(c => (c, new List<GridRowAction> { new("Sửa", Url.Action(nameof(Edit), new { id = c.Id })!) }
                .Concat(canSeeProducts ? [new GridRowAction("Xem sản phẩm", Url.Action("Index", "Products", new { category = c.Id })!)] : [])
                .ToList())).ToList(),
        });
    }

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

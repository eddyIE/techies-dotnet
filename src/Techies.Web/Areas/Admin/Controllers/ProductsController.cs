using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Services;
using Techies.Web.ViewModels.Catalog;

namespace Techies.Web.Areas.Admin.Controllers;

[RequirePermission(Permissions.ProductsView)]
public sealed class ProductsController(ProductService products, CategoryService categories, ImageStorage images)
    : AdminController
{
    public async Task<IActionResult> Index([FromQuery] ProductFilter filter)
    {
        ViewData["Filter"] = filter;
        ViewData["Categories"] = await categories.ListAsync();
        return View(await products.SearchAsync(filter));
    }

    [HttpGet, RequirePermission(Permissions.ProductsEdit)]
    public Task<IActionResult> Create() => FormView(new ProductForm(), id: null);

    [HttpPost, RequirePermission(Permissions.ProductsEdit)]
    public async Task<IActionResult> Create(ProductForm form)
    {
        if (!ModelState.IsValid) return await FormView(form, null);
        try
        {
            var (thumbnail, gallery) = await SaveUploadsAsync(form);
            var id = await products.CreateAsync(form, thumbnail, gallery);
            TempData["Success"] = "Đã thêm sản phẩm. Hãy nhập hàng để có tồn kho.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return await FormView(form, null);
        }
    }

    [HttpGet, RequirePermission(Permissions.ProductsEdit)]
    public async Task<IActionResult> Edit(Guid id)
    {
        var p = await products.GetAsync(id);
        return await FormView(new ProductForm
        {
            Name = p.Name, CategoryId = p.CategoryId, Price = p.Price, Description = p.Description, Active = p.Active,
            CurrentThumbnailUrl = p.ThumbnailUrl,
            ExistingImages = p.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url).ToList(),
            Specs = p.Specs.OrderBy(s => s.DisplayOrder).Select(s => new SpecInput { Name = s.SpecName, Value = s.SpecValue }).ToList(),
        }, id);
    }

    [HttpPost, RequirePermission(Permissions.ProductsEdit)]
    public async Task<IActionResult> Edit(Guid id, ProductForm form)
    {
        if (!ModelState.IsValid) return await FormView(form, id);
        try
        {
            var (thumbnail, gallery) = await SaveUploadsAsync(form);
            await products.UpdateAsync(id, form, thumbnail, gallery);
            TempData["Success"] = "Đã lưu sản phẩm.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return await FormView(form, id);
        }
    }

    private async Task<(string? Thumbnail, List<string> Gallery)> SaveUploadsAsync(ProductForm form)
    {
        var thumbnail = form.Thumbnail is null ? null : await images.SaveAsync(form.Thumbnail);
        var gallery = new List<string>();
        foreach (var file in form.NewImages) gallery.Add(await images.SaveAsync(file));
        return (thumbnail, gallery);
    }

    private async Task<IActionResult> FormView(ProductForm form, Guid? id)
    {
        ViewData["Id"] = id;
        ViewData["Categories"] = await categories.ListAsync();
        return View("Edit", form);
    }
}

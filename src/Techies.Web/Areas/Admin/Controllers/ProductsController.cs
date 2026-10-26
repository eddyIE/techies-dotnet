using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.Services;
using Techies.Web.ViewModels.Catalog;

namespace Techies.Web.Areas.Admin.Controllers;

[RequirePermission(Permissions.ProductsView)]
public sealed class ProductsController(ProductService products, CategoryService categories, ImageStorage images, CurrentStaff staff)
    : AdminController
{
    public async Task<IActionResult> Index()
    {
        var request = GridRequest.From(Request.Path, Request.QueryString, ProductService.GridSorts, "name", false);
        var (rows, total) = await products.GridAsync(request);
        var access = await staff.AccessAsync();
        var canToggle = access.Has(Permissions.ProductsToggle);
        return GridView(new GridModel
        {
            Id = "products", Request = request, Total = total, RowsPartial = "_ProductRows",
            KeywordPlaceholder = "Tìm theo tên sản phẩm...",
            Columns = [new(""), new("Sản phẩm", "name"), new("Danh mục", "category"), new("Giá bán", "price", "text-end"),
                new("Tồn kho", "stock", "text-end"), new("Trạng thái")],
            Filters =
            [
                new("category", "Danh mục", GridFilterType.Select,
                    (await categories.ListAsync()).Select(c => new GridOption(c.Id.ToString(), c.Name)).ToList()),
                new("price", "Giá bán (VNĐ)", GridFilterType.NumberRange, Money: true),
                new("stock", "Tồn kho", GridFilterType.NumberRange),
                new("active", "Kinh doanh", GridFilterType.Select, [new("true", "Đang bán"), new("false", "Ngừng bán")]),
            ],
            MassActions = canToggle
                ? [new("Bật kinh doanh", Url.Action(nameof(MassEnable))!), new("Tắt kinh doanh", Url.Action(nameof(MassDisable))!)]
                : [],
            Rows = rows.Select(p => (p, RowActions(p, access))).ToList(),
        });
    }

    private List<GridRowAction> RowActions(ProductRow p, StaffAccess access)
    {
        var actions = new List<GridRowAction>();
        if (access.Has(Permissions.ProductsEdit)) actions.Add(new("Sửa", Url.Action(nameof(Edit), new { id = p.Id })!));
        if (access.Has(Permissions.ProductsToggle))
            actions.Add(new(p.Active ? "Tắt kinh doanh" : "Bật kinh doanh", Url.Action(nameof(ToggleActive), new { id = p.Id })!, Post: true));
        if (access.Has(Permissions.InventoryView))
            actions.Add(new("Tồn kho & nhập hàng", Url.Action("Details", "Inventory", new { id = p.Id })!));
        if (p.Active) actions.Add(new("Xem trên cửa hàng", "/san-pham/" + p.Slug, NewTab: true));
        return actions;
    }

    [HttpPost, RequirePermission(Permissions.ProductsToggle)]
    public Task<IActionResult> MassEnable(List<Guid> ids, string? returnUrl) =>
        MassAsync(ids, id => products.SetActiveAsync(id, true), "bật kinh doanh", returnUrl);

    [HttpPost, RequirePermission(Permissions.ProductsToggle)]
    public Task<IActionResult> MassDisable(List<Guid> ids, string? returnUrl) =>
        MassAsync(ids, id => products.SetActiveAsync(id, false), "tắt kinh doanh", returnUrl);

    [HttpPost, RequirePermission(Permissions.ProductsToggle)]
    public async Task<IActionResult> ToggleActive(Guid id, string? returnUrl)
    {
        var product = await products.GetAsync(id);
        return await MassAsync([id], x => products.SetActiveAsync(x, !product.Active), product.Active ? "tắt kinh doanh" : "bật kinh doanh", returnUrl);
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

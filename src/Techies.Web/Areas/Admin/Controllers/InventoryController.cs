using Microsoft.AspNetCore.Mvc;
using Techies.Web.Infrastructure;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.Services;

namespace Techies.Web.Areas.Admin.Controllers;

[RequirePermission(Permissions.InventoryView)]
public sealed class InventoryController(ProductService products, CategoryService categories, InventoryService inventory,
    StockLedger ledger, CurrentStaff staff) : AdminController
{
    public async Task<IActionResult> Index()
    {
        var request = GridRequest.From(Request.Path, Request.QueryString, ProductService.GridSorts, "stock", false);
        var (rows, total) = await products.GridAsync(request);
        var access = await staff.AccessAsync();
        var canChange = access.Has(Permissions.InventoryImport) || access.Has(Permissions.InventoryCount);
        return GridView(new GridModel
        {
            Id = "inventory", Request = request, Total = total, RowsPartial = "_InventoryRows",
            KeywordPlaceholder = "Tìm sản phẩm...",
            Columns = [new("Sản phẩm", "name"), new("Danh mục", "category"), new("Tồn khả dụng", "stock", "text-end"), new("Kinh doanh")],
            Filters =
            [
                new("category", "Danh mục", GridFilterType.Select,
                    (await categories.ListAsync()).Select(c => new GridOption(c.Id.ToString(), c.Name)).ToList()),
                new("stock", "Tồn kho", GridFilterType.NumberRange),
                new("active", "Kinh doanh", GridFilterType.Select, [new("true", "Đang bán"), new("false", "Ngừng bán")]),
            ],
            Rows = rows.Select(p => (p, new List<GridRowAction>
            {
                new(canChange ? "Nhập hàng / Kiểm kê" : "Lịch sử kho", Url.Action(nameof(Details), new { id = p.Id })!),
            }.Concat(access.Has(Permissions.ProductsView) && access.Has(Permissions.ProductsEdit) ? [new GridRowAction("Sửa sản phẩm", Url.Action("Edit", "Products", new { id = p.Id })!)] : [])
             .ToList())).ToList(),
        });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        ViewData["Product"] = await products.GetAsync(id);
        ViewData["Available"] = await ledger.CurrentAsync(id);
        return View(await inventory.HistoryAsync(id));
    }

    [HttpPost, RequirePermission(Permissions.InventoryImport)]
    public Task<IActionResult> Import(Guid id, int quantity, string? reason) =>
        TryAsync(() => inventory.ImportAsync(id, quantity, reason, StaffId),
            $"Đã nhập thêm {quantity} sản phẩm.", RedirectToAction(nameof(Details), new { id }));

    [HttpPost, RequirePermission(Permissions.InventoryCount)]
    public Task<IActionResult> Count(Guid id, int quantity, string? reason) =>
        TryAsync(() => inventory.CountAsync(id, quantity, reason, StaffId),
            $"Đã cập nhật tồn kho thực tế: {quantity}.", RedirectToAction(nameof(Details), new { id }));
}

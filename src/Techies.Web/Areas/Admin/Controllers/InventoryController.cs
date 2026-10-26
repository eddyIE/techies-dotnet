using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Infrastructure;
using Techies.Web.Services;
using Techies.Web.ViewModels.Catalog;

namespace Techies.Web.Areas.Admin.Controllers;

[RequirePermission(Permissions.InventoryView)]
public sealed class InventoryController(ProductService products, InventoryService inventory, StockLedger ledger)
    : AdminController
{
    public async Task<IActionResult> Index([FromQuery] ProductFilter filter)
    {
        ViewData["Filter"] = filter;
        return View(await products.SearchAsync(filter));
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

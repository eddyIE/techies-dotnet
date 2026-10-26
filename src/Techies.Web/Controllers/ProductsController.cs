using Microsoft.AspNetCore.Mvc;
using Techies.Web.Services;

namespace Techies.Web.Controllers;

public sealed class ProductsController(ShopQueries shop) : Controller
{
    [HttpGet("san-pham")]
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        ViewData["Q"] = q;
        ViewData["Heading"] = string.IsNullOrWhiteSpace(q) ? "Tất cả sản phẩm" : $"Kết quả tìm kiếm cho “{q}”";
        return View(await shop.ListAsync(null, q, page));
    }

    [HttpGet("danh-muc/{slug}")]
    public async Task<IActionResult> Category(string slug, int page = 1)
    {
        var category = await shop.CategoryAsync(slug);
        if (category is null) return NotFound();
        ViewData["Heading"] = category.Name;
        return View("Index", await shop.ListAsync(slug, null, page));
    }

    [HttpGet("san-pham/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var detail = await shop.DetailAsync(slug);
        return detail is null ? NotFound() : View(detail);
    }
}

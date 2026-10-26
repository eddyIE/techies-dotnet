using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Infrastructure;
using Techies.Web.Services;
using Techies.Web.ViewModels.Catalog;

namespace Techies.Web.Areas.Admin.Controllers;

[Authorize(Policy = Policies.Catalog)]
public sealed class ProductsController(ProductService products, CategoryService categories) : AdminController
{
    public async Task<IActionResult> Index([FromQuery] ProductFilter filter)
    {
        ViewData["Filter"] = filter;
        ViewData["Categories"] = await categories.ListAsync();
        return View(await products.SearchAsync(filter));
    }
}

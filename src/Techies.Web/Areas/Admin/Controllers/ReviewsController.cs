using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Infrastructure;
using Techies.Web.Services;
using Techies.Web.ViewModels.Customers;

namespace Techies.Web.Areas.Admin.Controllers;

[RequirePermission(Permissions.ReviewsView)]
public sealed class ReviewsController(ReviewQueries reviews) : AdminController
{
    public async Task<IActionResult> Index([FromQuery] ReviewFilter filter)
    {
        ViewData["Filter"] = filter;
        ViewData["Products"] = await reviews.ReviewedProductsAsync();
        return View(await reviews.ListAsync(filter));
    }
}

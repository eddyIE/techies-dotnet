using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;

namespace Techies.Web.Areas.Admin.Controllers;

// Every admin controller derives from this: staff scheme required; controllers narrow the policy.
[Area("Admin")]
[Authorize(Policy = Policies.AnyStaff)]
public abstract class AdminController : Controller
{
    protected Guid StaffId => User.UserId();

    // A GET for a record that doesn't exist ("Không tìm thấy ...") is a 404, not a server error.
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Exception is DomainException && HttpMethods.IsGet(context.HttpContext.Request.Method))
        {
            context.Result = NotFound();
            context.ExceptionHandled = true;
        }
    }

    // A grid page: the full view, or just the grid when admin-grid.js asks for it.
    protected IActionResult GridView(Infrastructure.Grid.GridModel grid) =>
        Request.Headers.ContainsKey("X-Grid") ? PartialView("_Grid", grid) : View(grid);

    // Runs a mass action over the selected rows and returns to the grid with its filters.
    protected async Task<IActionResult> MassAsync<TId>(IReadOnlyCollection<TId> ids, Func<TId, Task> action, string verb, string? returnUrl)
    {
        if (ids.Count == 0) TempData["Error"] = "Chưa chọn dòng nào.";
        else
        {
            var result = await Infrastructure.Grid.Bulk.RunAsync(ids, action);
            TempData[result.Errors.Count == 0 ? "Success" : result.Done == 0 ? "Error" : "Success"] = result.Summary(verb);
        }
        return BackTo(returnUrl);
    }

    // Only ever back into the admin area (returnUrl comes from the client).
    protected IActionResult BackTo(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) && returnUrl!.StartsWith("/admin/", StringComparison.OrdinalIgnoreCase)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Index", new { area = "Admin" }); // explicit area: the shop has a ProductsController too

    // Runs a state change and turns a rule violation into a flash message.
    protected async Task<IActionResult> TryAsync(Func<Task> action, string success, IActionResult next)
    {
        try
        {
            await action();
            TempData["Success"] = success;
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return next;
    }
}

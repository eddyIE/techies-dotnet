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

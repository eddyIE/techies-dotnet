using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;

namespace Techies.Web.Areas.Admin.Controllers;

// Every admin controller derives from this: staff scheme required; controllers narrow the policy.
[Area("Admin")]
[Authorize(Policy = Policies.AnyStaff)]
public abstract class AdminController : Controller
{
    protected Guid StaffId => User.UserId();

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

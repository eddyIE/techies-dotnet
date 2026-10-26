using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Services;
using Techies.Web.ViewModels.Account;

namespace Techies.Web.Areas.Admin.Controllers;

[AllowAnonymous]
public sealed class AuthController(StaffAuthService auth) : AdminController
{
    [HttpGet]
    public IActionResult Login(string? returnUrl) => View(new LoginForm { ReturnUrl = returnUrl });

    [HttpPost]
    public async Task<IActionResult> Login(LoginForm form)
    {
        if (!ModelState.IsValid) return View(form);
        try
        {
            var staff = await auth.AuthenticateAsync(form.Email, form.Password);
            var principal = AuthSetup.StaffPrincipal(staff);
            await HttpContext.SignInAsync(AuthSchemes.Staff, principal);
            if (Url.IsLocalUrl(form.ReturnUrl) && form.ReturnUrl!.StartsWith("/admin"))
                return LocalRedirect(form.ReturnUrl);
            return RedirectToAction("Index", "Dashboard");
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(form);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AuthSchemes.Staff);
        return RedirectToAction(nameof(Login));
    }

    public IActionResult Denied() => View();
}

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Services;
using Techies.Web.ViewModels.Account;

namespace Techies.Web.Controllers;

[Route("tai-khoan")]
public sealed class AccountController(CustomerAccountService accounts) : Controller
{
    [HttpGet("dang-nhap")]
    public IActionResult Login(string? returnUrl) => View(new LoginForm { ReturnUrl = returnUrl });

    [HttpPost("dang-nhap")]
    public async Task<IActionResult> Login(LoginForm form)
    {
        if (!ModelState.IsValid) return View(form);
        try
        {
            var user = await accounts.AuthenticateAsync(form.Email, form.Password);
            await SignInAsync(user);
            return RedirectBack(form.ReturnUrl);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(form);
        }
    }

    [HttpGet("dang-ky")]
    public IActionResult Register() => View(new RegisterForm());

    [HttpPost("dang-ky")]
    public async Task<IActionResult> Register(RegisterForm form)
    {
        if (!ModelState.IsValid) return View(form);
        try
        {
            var user = await accounts.RegisterAsync(form);
            await SignInAsync(user);
            TempData["Success"] = "Đăng ký thành công. Chào mừng bạn đến với Techies!";
            return RedirectToAction("Index", "Home");
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(nameof(form.Email), ex.Message);
            return View(form);
        }
    }

    [HttpPost("dang-xuat")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AuthSchemes.Customer);
        return RedirectToAction("Index", "Home");
    }

    private Task SignInAsync(User user) =>
        HttpContext.SignInAsync(AuthSchemes.Customer, AuthSetup.CustomerPrincipal(user),
            new AuthenticationProperties { IsPersistent = true });

    // Only shop URLs: a customer login must never bounce into /admin.
    private IActionResult RedirectBack(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) && !returnUrl!.StartsWith("/admin", StringComparison.OrdinalIgnoreCase)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Index", "Home");
}

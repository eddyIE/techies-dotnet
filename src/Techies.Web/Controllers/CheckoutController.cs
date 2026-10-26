using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Services;
using Techies.Web.ViewModels.Shop;

namespace Techies.Web.Controllers;

[Route("thanh-toan")]
[Authorize(Policy = Policies.Customer)]
public sealed class CheckoutController(CartService carts, CheckoutService checkout, CustomerAccountService accounts) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var cart = CartService.Load(HttpContext.Session);
        var view = await carts.ViewAsync(cart);
        if (view.Lines.Count == 0) return RedirectToAction("Index", "Cart");
        var addresses = await accounts.AddressesAsync(User.UserId());
        return await FormView(new CheckoutForm { AddressId = addresses.FirstOrDefault()?.Id }, view);
    }

    [HttpPost("")]
    public async Task<IActionResult> Index(CheckoutForm form)
    {
        var cart = CartService.Load(HttpContext.Session);
        var view = await carts.ViewAsync(cart);
        if (view.Lines.Count == 0) return RedirectToAction("Index", "Cart");
        if (!ModelState.IsValid) return await FormView(form, view);

        Order order;
        try
        {
            order = await checkout.PlaceOrderAsync(User.UserId(), cart.Lines, form);
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Index", "Cart");
        }
        CartService.Save(HttpContext.Session, new Cart());
        return RedirectToAction(nameof(Success), new { orderRef = order.OrderRef });
    }

    [HttpGet("thanh-cong/{orderRef}")]
    public IActionResult Success(string orderRef) => View((object)orderRef);

    private async Task<IActionResult> FormView(CheckoutForm form, CartView cart)
    {
        ViewData["Cart"] = cart;
        ViewData["Addresses"] = await accounts.AddressesAsync(User.UserId());
        return View("Index", form);
    }
}

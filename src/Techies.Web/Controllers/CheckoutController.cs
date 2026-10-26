using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Services;
using Techies.Web.ViewModels.Shop;

namespace Techies.Web.Controllers;

[Route("thanh-toan")]
[Authorize(Policy = Policies.Customer)]
public sealed class CheckoutController(CartService carts, CheckoutService checkout, CustomerAccountService accounts,
    CouponService coupons) : Controller
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
    public async Task<IActionResult> Index(CheckoutForm form, bool apply = false)
    {
        var cart = CartService.Load(HttpContext.Session);
        var view = await carts.ViewAsync(cart);
        if (view.Lines.Count == 0) return RedirectToAction("Index", "Cart");

        // "Áp dụng" only previews the coupon; it never places the order.
        var coupon = await coupons.CheckAsync(form.CouponCode, view.Subtotal);
        if (apply)
        {
            ModelState.Clear();
            return await FormView(form, view, coupon);
        }
        if (coupon.Code is not null && !coupon.Applicable)
            ModelState.AddModelError(nameof(form.CouponCode), coupon.Message!);
        if (!ModelState.IsValid) return await FormView(form, view, coupon);

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

    private async Task<IActionResult> FormView(CheckoutForm form, CartView cart, CouponCheck? coupon = null)
    {
        ViewData["Cart"] = cart;
        ViewData["Coupon"] = coupon;
        ViewData["Addresses"] = await accounts.AddressesAsync(User.UserId());
        return View("Index", form);
    }
}

using Microsoft.AspNetCore.Mvc;
using Techies.Web.Domain;
using Techies.Web.Services;

namespace Techies.Web.Controllers;

[Route("gio-hang")]
public sealed class CartController(CartService carts) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var cart = CartService.Load(HttpContext.Session);
        var view = await carts.ViewAsync(cart);
        CartService.Save(HttpContext.Session, cart); // drops lines for products no longer sold
        return View(view);
    }

    [HttpPost("them")]
    public async Task<IActionResult> Add(Guid productId, int quantity = 1)
    {
        try
        {
            var cart = CartService.Load(HttpContext.Session);
            cart.Add(productId, quantity, await carts.AvailableAsync(productId));
            CartService.Save(HttpContext.Session, cart);
            TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("cap-nhat")]
    public async Task<IActionResult> Update(Guid productId, int quantity)
    {
        try
        {
            var cart = CartService.Load(HttpContext.Session);
            cart.Set(productId, quantity, quantity <= 0 ? 0 : await carts.AvailableAsync(productId));
            CartService.Save(HttpContext.Session, cart);
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("xoa")]
    public IActionResult Remove(Guid productId)
    {
        var cart = CartService.Load(HttpContext.Session);
        cart.Remove(productId);
        CartService.Save(HttpContext.Session, cart);
        return RedirectToAction(nameof(Index));
    }
}

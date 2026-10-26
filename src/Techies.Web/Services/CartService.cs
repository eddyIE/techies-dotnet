using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;

namespace Techies.Web.Services;

// The shop cart: product id -> quantity, kept in the session. Prices are never stored here;
// they are re-read from the catalog every time the cart is shown or checked out.
public sealed class Cart
{
    public const int MaxQuantity = 99;

    public Dictionary<Guid, int> Lines { get; init; } = [];

    public int Count => Lines.Values.Sum();

    public void Add(Guid productId, int quantity, int available) =>
        Set(productId, Lines.GetValueOrDefault(productId) + Math.Max(1, quantity), available);

    public void Set(Guid productId, int quantity, int available)
    {
        if (quantity <= 0)
        {
            Lines.Remove(productId);
            return;
        }
        if (available <= 0) throw new DomainException("Sản phẩm đã hết hàng.");
        Lines[productId] = Math.Min(Math.Min(quantity, MaxQuantity), available);
    }

    public void Remove(Guid productId) => Lines.Remove(productId);

    public string ToJson() => JsonSerializer.Serialize(Lines);

    public static Cart FromJson(string? json) =>
        string.IsNullOrEmpty(json) ? new Cart() : new Cart { Lines = JsonSerializer.Deserialize<Dictionary<Guid, int>>(json) ?? [] };
}

public sealed record CartLine(Guid ProductId, string Name, string Slug, string? ThumbnailUrl, decimal UnitPrice,
    int Quantity, int Available)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed record CartView(List<CartLine> Lines, decimal Subtotal, decimal ShippingFee, decimal Total);

public sealed class CartService(AppDbContext db)
{
    private const string SessionKey = "cart";

    public static Cart Load(ISession session) => Cart.FromJson(session.GetString(SessionKey));

    public static void Save(ISession session, Cart cart) => session.SetString(SessionKey, cart.ToJson());

    public async Task<int> AvailableAsync(Guid productId, CancellationToken ct = default) =>
        await db.Products.Where(p => p.Id == productId && p.Active).Select(p => (int?)p.Stock!.Available).SingleOrDefaultAsync(ct)
        ?? throw new DomainException("Sản phẩm không còn kinh doanh.");

    // Current prices and stock; products that were deactivated meanwhile drop out of the cart.
    public async Task<CartView> ViewAsync(Cart cart, CancellationToken ct = default)
    {
        var ids = cart.Lines.Keys.ToList();
        var products = await db.Products.AsNoTracking().Where(p => ids.Contains(p.Id) && p.Active)
            .Select(p => new { p.Id, p.Name, p.Slug, p.ThumbnailUrl, p.Price, Available = p.Stock!.Available })
            .ToListAsync(ct);
        foreach (var missing in ids.Except(products.Select(p => p.Id))) cart.Remove(missing);

        var lines = products.OrderBy(p => p.Name)
            .Select(p => new CartLine(p.Id, p.Name, p.Slug, p.ThumbnailUrl, p.Price, cart.Lines[p.Id], p.Available))
            .ToList();
        var subtotal = lines.Sum(l => l.LineTotal);
        var shipping = lines.Count == 0 ? 0 : OrderMath.ShippingFee(subtotal);
        return new CartView(lines, subtotal, shipping, OrderMath.Total(subtotal, shipping, 0));
    }
}

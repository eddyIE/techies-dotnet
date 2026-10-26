using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.ViewModels.Shop;

namespace Techies.Web.Services;

public sealed record ShippingAddress(string Recipient, string Phone, string Line1, string Ward, string District, string Province);

// The shop's "Đặt hàng" (COD only). All or nothing: either every line is reserved and the
// order exists, or nothing changed and the customer is told which product failed.
public sealed class CheckoutService(AppDbContext db, StockLedger ledger, CustomerAccountService accounts, TimeProvider clock)
{
    // From the checkout page: a newly entered address is saved in the same transaction, so a
    // failed checkout leaves the customer's address book untouched.
    public Task<Order> PlaceOrderAsync(Guid customerId, IReadOnlyDictionary<Guid, int> lines, CheckoutForm form,
        CancellationToken ct = default) =>
        PlaceAsync(customerId, lines, () => accounts.ResolveShippingAsync(customerId, form, ct), ct);

    public Task<Order> PlaceOrderAsync(Guid customerId, IReadOnlyDictionary<Guid, int> lines, ShippingAddress address,
        CancellationToken ct = default) =>
        PlaceAsync(customerId, lines, () => Task.FromResult(address), ct);

    private async Task<Order> PlaceAsync(Guid customerId, IReadOnlyDictionary<Guid, int> lines,
        Func<Task<ShippingAddress>> resolveAddress, CancellationToken ct)
    {
        if (lines.Count == 0 || lines.Values.Any(q => q <= 0))
            throw new DomainException("Giỏ hàng trống.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var address = await resolveAddress();
        var ids = lines.Keys.ToList();
        var products = await db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

        var now = clock.GetUtcNow();
        var order = new Order
        {
            UserId = customerId, Status = OrderStatus.Confirmed,
            PaymentMethod = PaymentMethod.Cod, PaymentStatus = PaymentStatus.Pending,
            ShipRecipient = address.Recipient, ShipPhone = address.Phone, ShipLine1 = address.Line1,
            ShipWard = address.Ward, ShipDistrict = address.District, ShipProvince = address.Province,
            CreatedAt = now, UpdatedAt = now,
        };

        // Fixed lock order (by product id) so two carts with the same products cannot deadlock.
        foreach (var (productId, quantity) in lines.OrderBy(l => l.Key))
        {
            if (!products.TryGetValue(productId, out var product) || !product.Active)
                throw new DomainException($"Sản phẩm “{product?.Name ?? "đã chọn"}” không còn kinh doanh.");
            try
            {
                await ledger.ChangeAsync(productId, -quantity, AdjustmentType.Order, "Đặt hàng", order.Id, ct: ct);
            }
            catch (DomainException)
            {
                var left = await ledger.CurrentAsync(productId, ct);
                throw new DomainException($"Sản phẩm “{product.Name}” chỉ còn {left} trong kho.");
            }
            order.Items.Add(new OrderItem
            {
                ProductId = productId, ProductName = product.Name, UnitPrice = product.Price,
                ThumbnailUrl = product.ThumbnailUrl, Quantity = quantity, LineTotal = product.Price * quantity,
            });
        }

        order.Subtotal = order.Items.Sum(i => i.LineTotal);
        order.ShippingFee = OrderMath.ShippingFee(order.Subtotal);
        order.Total = OrderMath.Total(order.Subtotal, order.ShippingFee, 0);
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return order;
    }
}

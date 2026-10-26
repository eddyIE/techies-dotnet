using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;

namespace Techies.Web.Services;

// BRD §3: the two ways a CONFIRMED order ends. Each runs in one transaction with the order
// row locked, so a double click or two staff acting at once cannot apply it twice.
public sealed class OrderService(AppDbContext db, LoyaltyService loyalty, StockLedger ledger, TimeProvider clock)
{
    // CONFIRMED -> COMPLETED: delivered and paid; award loyalty points.
    public async Task CompleteAsync(Guid orderId, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var order = await LockAsync(orderId, ct);
        if (order.Status != OrderStatus.Confirmed)
            throw new DomainException("Chỉ hoàn tất được đơn đang ở trạng thái Đã xác nhận.");

        var now = clock.GetUtcNow();
        order.Status = OrderStatus.Completed;
        order.CompletedAt = now;
        order.UpdatedAt = now;
        if (order.PaymentMethod == PaymentMethod.Cod)
            order.PaymentStatus = PaymentStatus.Paid; // the shipper collected the money

        await loyalty.AwardForOrderAsync(order, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public Task CancelByStaffAsync(Guid orderId, Guid? staffId, CancellationToken ct = default) =>
        CancelAsync(orderId, staffId, customerId: null, ct);

    // The shop's "Hủy đơn": same rule, but only on the customer's own order.
    public Task CancelByCustomerAsync(Guid orderId, Guid customerId, CancellationToken ct = default) =>
        CancelAsync(orderId, staffId: null, customerId, ct);

    // CONFIRMED -> CANCELLED: restock every item, give the coupon back, refund if prepaid.
    private async Task CancelAsync(Guid orderId, Guid? staffId, Guid? customerId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var order = await LockAsync(orderId, ct);
        if (customerId is not null && order.UserId != customerId)
            throw new DomainException("Không tìm thấy đơn hàng.");
        if (order.Status != OrderStatus.Confirmed)
            throw new DomainException("Chỉ hủy được đơn đang ở trạng thái Đã xác nhận.");

        var now = clock.GetUtcNow();
        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = now;
        if (order.PaymentStatus == PaymentStatus.Paid)
            order.PaymentStatus = PaymentStatus.Refunded;

        var items = await db.OrderItems.Where(i => i.OrderId == orderId).ToListAsync(ct);
        var reason = customerId is null ? $"Hủy đơn {order.OrderRef}" : $"Khách hủy đơn {order.OrderRef}";
        foreach (var item in items)
            await ledger.ChangeAsync(item.ProductId, item.Quantity, AdjustmentType.Cancel, reason, order.Id, staffId, ct);

        await db.CouponRedemptions
            .Where(r => r.OrderId == orderId && r.Status == CouponRedemptionStatus.Applied)
            .ExecuteUpdateAsync(u => u.SetProperty(r => r.Status, CouponRedemptionStatus.Restored).SetProperty(r => r.RestoredAt, now), ct);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private async Task<Order> LockAsync(Guid orderId, CancellationToken ct) =>
        await db.Orders.FromSql($"SELECT * FROM orders WHERE id = {orderId} FOR UPDATE").SingleOrDefaultAsync(ct)
        ?? throw new DomainException("Không tìm thấy đơn hàng.");
}

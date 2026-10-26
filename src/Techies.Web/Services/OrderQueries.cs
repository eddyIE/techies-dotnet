using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.ViewModels;
using Techies.Web.ViewModels.Orders;

namespace Techies.Web.Services;

public sealed class OrderQueries(AppDbContext db)
{
    public Task<Paged<OrderRow>> ListAsync(OrderFilter filter, CancellationToken ct = default)
    {
        var query = db.Orders.AsNoTracking();
        if (filter.Status is { } status)
            query = query.Where(o => o.Status == status);
        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var pattern = QueryExtensions.Contains(filter.Q);
            query = query.Where(o => EF.Functions.ILike(o.OrderRef, pattern) || EF.Functions.ILike(o.ShipPhone, pattern));
        }
        return query.OrderByDescending(o => o.CreatedAt).Select(ToRow).ToPagedAsync(filter.Page, ct: ct);
    }

    public async Task<Dictionary<OrderStatus, int>> CountsByStatusAsync(CancellationToken ct = default)
    {
        var counts = await db.Orders.GroupBy(o => o.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        return Enum.GetValues<OrderStatus>().ToDictionary(s => s, s => counts.SingleOrDefault(c => c.Key == s)?.Count ?? 0);
    }

    public async Task<Order> DetailAsync(Guid id, CancellationToken ct = default) =>
        await db.Orders.AsNoTracking().Include(o => o.Items).Include(o => o.User).SingleOrDefaultAsync(o => o.Id == id, ct)
        ?? throw new DomainException("Không tìm thấy đơn hàng.");

    // Shop "Đơn hàng của tôi": a customer only ever sees their own orders.
    public Task<Paged<OrderRow>> ForCustomerAsync(Guid customerId, int page, CancellationToken ct = default) =>
        db.Orders.AsNoTracking().Where(o => o.UserId == customerId).OrderByDescending(o => o.CreatedAt)
            .Select(ToRow).ToPagedAsync(page, 10, ct);

    public Task<Order?> ForCustomerDetailAsync(Guid customerId, Guid orderId, CancellationToken ct = default) =>
        db.Orders.AsNoTracking().Include(o => o.Items).SingleOrDefaultAsync(o => o.Id == orderId && o.UserId == customerId, ct);

    public static readonly System.Linq.Expressions.Expression<Func<Order, OrderRow>> ToRow = o =>
        new OrderRow(o.Id, o.OrderRef, o.ShipRecipient, o.ShipPhone, o.CreatedAt, o.Total, o.PaymentMethod, o.PaymentStatus, o.Status);
}

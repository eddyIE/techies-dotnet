using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.ViewModels;
using Techies.Web.ViewModels.Orders;

namespace Techies.Web.Services;

public sealed class OrderQueries(AppDbContext db)
{
    public static readonly string[] GridSorts = ["ref", "created", "total"];

    // Orders grid (SPEC orders + admin-grid). ?status=CONFIRMED deep links keep working.
    public async Task<(List<OrderRow> Rows, int Total)> GridAsync(GridRequest r, CancellationToken ct = default)
    {
        var query = db.Orders.AsNoTracking();
        if (r.Text("q") is { } q)
        {
            var pattern = QueryExtensions.Contains(q);
            query = query.Where(o => EF.Functions.ILike(o.OrderRef, pattern) || EF.Functions.ILike(o.ShipPhone, pattern));
        }
        if (r.Enum<OrderStatus>("status") is { } status) query = query.Where(o => o.Status == status);
        if (r.Enum<PaymentMethod>("method") is { } method) query = query.Where(o => o.PaymentMethod == method);
        if (r.Enum<PaymentStatus>("payment") is { } payment) query = query.Where(o => o.PaymentStatus == payment);
        if (r.Decimal("total_from") is { } totalFrom) query = query.Where(o => o.Total >= totalFrom);
        if (r.Decimal("total_to") is { } totalTo) query = query.Where(o => o.Total <= totalTo);
        if (r.Date("created_from") is { } from) { var start = Format.StartOfLocalDay(from); query = query.Where(o => o.CreatedAt >= start); }
        if (r.Date("created_to") is { } to) { var end = Format.StartOfLocalDay(to.AddDays(1)); query = query.Where(o => o.CreatedAt < end); }

        query = (r.Sort, r.Desc) switch
        {
            ("ref", false) => query.OrderBy(o => o.OrderRef),
            ("ref", true) => query.OrderByDescending(o => o.OrderRef),
            ("total", false) => query.OrderBy(o => o.Total).ThenByDescending(o => o.CreatedAt),
            ("total", true) => query.OrderByDescending(o => o.Total).ThenByDescending(o => o.CreatedAt),
            (_, false) => query.OrderBy(o => o.CreatedAt),
            _ => query.OrderByDescending(o => o.CreatedAt),
        };
        var total = await query.CountAsync(ct);
        var rows = await query.Skip((r.Page - 1) * r.PageSize).Take(r.PageSize).Select(ToRow).ToListAsync(ct);
        return (rows, total);
    }

    public Task<List<Order>> DetailsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) =>
        db.Orders.AsNoTracking().Include(o => o.Items).Where(o => ids.Contains(o.Id)).OrderBy(o => o.OrderRef).ToListAsync(ct);

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

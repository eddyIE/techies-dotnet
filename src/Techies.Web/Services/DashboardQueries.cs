using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.ViewModels.Orders;

namespace Techies.Web.Services;

public sealed record RevenueSummary(RevenuePeriod Period, decimal Total, decimal PreviousTotal, double? ChangePercent,
    List<string> Labels, List<decimal> Values);

public sealed record BestSeller(Guid ProductId, string Name, int Quantity, decimal Revenue, int Available);

// BRD §1. Revenue counts COMPLETED orders only, dated by when they completed.
public sealed class DashboardQueries(AppDbContext db)
{
    public const int LowStockThreshold = 5;

    public async Task<RevenueSummary> RevenueAsync(RevenuePeriod period, DateTimeOffset now, CancellationToken ct = default)
    {
        var range = RevenuePeriods.For(period, now);
        var completed = await db.Orders.AsNoTracking()
            .Where(o => o.Status == OrderStatus.Completed && o.CompletedAt >= range.Start && o.CompletedAt < range.End)
            .Select(o => new { At = o.CompletedAt!.Value, o.Total })
            .ToListAsync(ct);
        var previous = await db.Orders
            .Where(o => o.Status == OrderStatus.Completed && o.CompletedAt >= range.PreviousStart && o.CompletedAt < range.PreviousEnd)
            .SumAsync(o => o.Total, ct);

        var values = range.Buckets.Select(b => completed.Where(o => o.At >= b.Start && o.At < b.End).Sum(o => o.Total)).ToList();
        var total = completed.Sum(o => o.Total);
        return new RevenueSummary(period, total, previous, RevenuePeriods.PercentChange(total, previous),
            range.Buckets.Select(b => b.Label).ToList(), values);
    }

    public async Task<List<BestSeller>> BestSellersAsync(RevenuePeriod period, DateTimeOffset now, CancellationToken ct = default)
    {
        var range = RevenuePeriods.For(period, now);
        var sold = await db.OrderItems.AsNoTracking()
            .Where(i => db.Orders.Any(o => o.Id == i.OrderId && o.Status == OrderStatus.Completed
                && o.CompletedAt >= range.Start && o.CompletedAt < range.End))
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.LineTotal) })
            .OrderByDescending(x => x.Quantity).ThenByDescending(x => x.Revenue)
            .Take(10)
            .ToListAsync(ct);
        var ids = sold.Select(s => s.ProductId).ToList();
        var products = await db.Products.AsNoTracking().Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, Available = p.Stock!.Available }).ToDictionaryAsync(p => p.Id, ct);
        return sold.Select(s => new BestSeller(s.ProductId, products[s.ProductId].Name, s.Quantity, s.Revenue, products[s.ProductId].Available)).ToList();
    }

    public async Task<(int AwaitingPayment, int Confirmed)> PendingCountsAsync(CancellationToken ct = default) => (
        await db.Orders.CountAsync(o => o.Status == OrderStatus.AwaitingPayment, ct),
        await db.Orders.CountAsync(o => o.Status == OrderStatus.Confirmed, ct));

    public Task<List<OrderRow>> RecentOrdersAsync(CancellationToken ct = default) =>
        db.Orders.AsNoTracking().OrderByDescending(o => o.CreatedAt).Take(10).Select(OrderQueries.ToRow).ToListAsync(ct);
}

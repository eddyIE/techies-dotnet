using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.ViewModels.Loyalty;

namespace Techies.Web.Services;

// BRD §5: membership tiers and points.
public sealed class LoyaltyService(AppDbContext db, TimeProvider clock)
{
    public static short TierFor(long lifetimePoints, IEnumerable<MembershipTier> tiers) =>
        tiers.Where(t => t.ThresholdPoints <= lifetimePoints).Max(t => t.Tier);

    // Called inside OrderService.CompleteAsync's transaction; the caller saves. The filtered
    // unique index on point_transactions(order_id) backs up the "never twice" check.
    public async Task AwardForOrderAsync(Order order, CancellationToken ct = default)
    {
        if (await db.PointTransactions.AnyAsync(t => t.OrderId == order.Id && t.TransactionType == PointTransactionType.Earn, ct))
            return;
        var points = OrderMath.PointsFor(order.Subtotal, order.Discount);
        if (points <= 0) return;

        var loyalty = await db.CustomerLoyalty
            .FromSql($"SELECT * FROM customer_loyalty WHERE customer_id = {order.UserId} FOR UPDATE")
            .SingleAsync(ct);
        var now = clock.GetUtcNow();
        loyalty.LifetimePoints += points;
        loyalty.Balance += points;
        loyalty.Tier = TierFor(loyalty.LifetimePoints, await db.MembershipTiers.ToListAsync(ct));
        loyalty.UpdatedAt = now;
        db.PointTransactions.Add(new PointTransaction
        {
            CustomerId = order.UserId, TransactionType = PointTransactionType.Earn, PointsDelta = points,
            OrderId = order.Id, Note = $"Hoàn tất đơn {order.OrderRef}", CreatedAt = now,
        });
    }

    public Task<List<MembershipTier>> TiersAsync(CancellationToken ct = default) =>
        db.MembershipTiers.AsNoTracking().OrderBy(t => t.Tier).ToListAsync(ct);

    // Tier 0 is fixed at 0 points; tiers 1–3 are edited together. Every customer's tier is
    // recomputed from lifetime points so the new policy applies to everyone at once.
    public async Task UpdateTiersAsync(IReadOnlyList<TierInput> inputs, CancellationToken ct = default)
    {
        var ordered = inputs.OrderBy(i => i.Tier).ToList();
        if (ordered.Select(i => (int)i.Tier).SequenceEqual([1, 2, 3]) is false)
            throw new DomainException("Phải cấu hình đủ hạng 1, 2 và 3.");
        if (ordered.Any(i => string.IsNullOrWhiteSpace(i.Name)))
            throw new DomainException("Tên hạng không được để trống.");
        if (ordered.Any(i => i.DiscountPercent is < 0 or > 100))
            throw new DomainException("Mức ưu đãi phải từ 0 đến 100%.");
        long previous = 0;
        foreach (var input in ordered)
        {
            if (input.ThresholdPoints <= previous)
                throw new DomainException("Mốc điểm phải tăng dần: hạng sau cao hơn hạng trước và lớn hơn 0.");
            previous = input.ThresholdPoints;
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var tiers = await db.MembershipTiers.Where(t => t.Tier > 0).ToListAsync(ct);
        foreach (var tier in tiers)
        {
            var input = ordered.Single(i => i.Tier == tier.Tier);
            tier.Name = input.Name.Trim();
            tier.ThresholdPoints = input.ThresholdPoints;
            tier.DiscountPercent = input.DiscountPercent;
        }
        await db.SaveChangesAsync(ct);
        var now = clock.GetUtcNow();
        await db.Database.ExecuteSqlAsync($"""
            UPDATE customer_loyalty cl
            SET tier = (SELECT max(t.tier) FROM membership_tiers t WHERE t.threshold_points <= cl.lifetime_points),
                updated_at = {now}
            """, ct);
        await tx.CommitAsync(ct);
    }
}

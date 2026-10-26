using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Data.Seed;
using Techies.Web.Domain;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class OrderSeedTests(PostgresFixture fixture)
{
    private async Task<AppDbContext> SeededAsync()
    {
        var db = await fixture.CreateContextAsync();
        await new DbSeeder(db, TimeProvider.System).SeedAsync();
        return db;
    }

    [Fact]
    public async Task Every_status_payment_method_and_failure_code_is_present()
    {
        await using var db = await SeededAsync();

        var statuses = await db.Orders.Select(o => o.Status).Distinct().ToListAsync();
        var methods = await db.Orders.Select(o => o.PaymentMethod).Distinct().ToListAsync();
        var payments = await db.Orders.Select(o => o.PaymentStatus).Distinct().ToListAsync();
        var failures = await db.Orders.Where(o => o.FailureCode != null).Select(o => o.FailureCode!).Distinct().ToListAsync();

        Assert.Equal(Enum.GetValues<OrderStatus>().Order(), statuses.Order());
        Assert.Equal(Enum.GetValues<PaymentMethod>().Order(), methods.Order());
        Assert.Equal(Enum.GetValues<PaymentStatus>().Order(), payments.Order());
        Assert.Equal(["OUT_OF_STOCK", "PAYMENT_FAILED", "SERVICE_UNAVAILABLE"], failures.Order());
        Assert.True(await db.CouponRedemptions.AnyAsync());
    }

    [Fact]
    public async Task Loyalty_points_match_the_point_ledger()
    {
        await using var db = await SeededAsync();

        var accounts = await db.CustomerLoyalty.ToListAsync();
        var ledger = await db.PointTransactions.GroupBy(t => t.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Earned = g.Where(t => t.TransactionType == PointTransactionType.Earn).Sum(t => t.PointsDelta),
                Net = g.Sum(t => t.PointsDelta),
            }).ToDictionaryAsync(x => x.CustomerId);

        foreach (var account in accounts)
        {
            var entry = ledger.GetValueOrDefault(account.CustomerId);
            Assert.Equal(entry?.Earned ?? 0, account.LifetimePoints);
            Assert.Equal(entry?.Net ?? 0, account.Balance);
        }
        Assert.Equal(await db.Orders.CountAsync(o => o.Status == OrderStatus.Completed),
            await db.PointTransactions.CountAsync(t => t.TransactionType == PointTransactionType.Earn));
    }

    [Fact]
    public async Task Every_review_belongs_to_a_completed_order_containing_the_product()
    {
        await using var db = await SeededAsync();

        Assert.Equal(182, await db.Reviews.CountAsync());
        var orphans = await db.Reviews.CountAsync(r => !db.Orders.Any(o =>
            o.Id == r.OrderId && o.UserId == r.CustomerId && o.Status == OrderStatus.Completed &&
            o.Items.Any(i => i.ProductId == r.ProductId)));
        Assert.Equal(0, orphans);
    }

    [Fact]
    public async Task Gift_redemptions_include_issued_and_claimed_with_brd_example_code()
    {
        await using var db = await SeededAsync();

        Assert.True(await db.GiftRedemptions.AnyAsync(r => r.Status == GiftRedemptionStatus.Claimed && r.ClaimedBy != null));
        Assert.True(await db.GiftRedemptions.AnyAsync(r => r.Code == "GIFT-G53Y-ZDS3" && r.Status == GiftRedemptionStatus.Issued));
    }
}

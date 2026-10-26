using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Services;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class RewardsServiceTests(PostgresFixture fixture)
{
    private static RewardsService Service(AppDbContext db) => new(db, TimeProvider.System);

    private static async Task<(User Customer, Gift Gift)> ArrangeAsync(AppDbContext db, long balance = 2_000, short tier = 1,
        long cost = 800, short minTier = 0, int stock = 5, bool active = true)
    {
        TestData.Tiers(db);
        var customer = TestData.Customer(db, lifetimePoints: 15_000);
        customer.Loyalty!.Balance = balance;
        customer.Loyalty.Tier = tier;
        var gift = new Gift { Name = "Cáp sạc", PointsCost = cost, MinTier = minTier, StockAvailable = stock, Active = active };
        db.Gifts.Add(gift);
        await db.SaveChangesAsync();
        return (customer, gift);
    }

    [Fact]
    public async Task Redeeming_spends_balance_takes_stock_and_issues_a_code()
    {
        await using var db = await fixture.CreateContextAsync();
        var (customer, gift) = await ArrangeAsync(db);

        var redemption = await Service(db).RedeemAsync(customer.Id, gift.Id);

        db.ChangeTracker.Clear();
        Assert.Matches(@"^GIFT-[A-Z2-9]{4}-[A-Z2-9]{4}$", redemption.Code);
        var loyalty = await db.CustomerLoyalty.SingleAsync();
        Assert.Equal((1_200L, 15_000L, (short)1), (loyalty.Balance, loyalty.LifetimePoints, loyalty.Tier)); // lifetime & tier unchanged
        Assert.Equal(4, (await db.Gifts.SingleAsync()).StockAvailable);
        var saved = await db.GiftRedemptions.SingleAsync();
        Assert.Equal((GiftRedemptionStatus.Issued, 800L), (saved.Status, saved.PointsSpent));
        var tx = await db.PointTransactions.SingleAsync();
        Assert.Equal((PointTransactionType.Redeem, -800L, (Guid?)saved.Id), (tx.TransactionType, tx.PointsDelta, tx.GiftRedemptionId));
    }

    [Theory]
    [InlineData(500, 1, 800, 0, 5, true, "không đủ điểm")]
    [InlineData(5_000, 0, 800, 2, 5, true, "hạng")]
    [InlineData(5_000, 1, 800, 0, 0, true, "hết")]
    [InlineData(5_000, 1, 800, 0, 5, false, "không còn")]
    public async Task Ineligible_redemptions_are_rejected_with_the_reason(long balance, short tier, long cost, short minTier, int stock, bool active, string reason)
    {
        await using var db = await fixture.CreateContextAsync();
        var (customer, gift) = await ArrangeAsync(db, balance, tier, cost, minTier, stock, active);

        var ex = await Assert.ThrowsAsync<DomainException>(() => Service(db).RedeemAsync(customer.Id, gift.Id));

        Assert.Contains(reason, ex.Message, StringComparison.OrdinalIgnoreCase);
        db.ChangeTracker.Clear();
        Assert.Equal(balance, (await db.CustomerLoyalty.SingleAsync()).Balance);
        Assert.Equal(0, await db.GiftRedemptions.CountAsync());
    }

    [Fact]
    public async Task Concurrent_redemptions_of_the_last_gift_issue_it_once()
    {
        await using var db = await fixture.CreateContextAsync();
        var (lan, gift) = await ArrangeAsync(db, stock: 1);
        var minh = TestData.Customer(db, "minh@example.com", lifetimePoints: 15_000);
        minh.Loyalty!.Balance = 2_000;
        minh.Loyalty.Tier = 1;
        await db.SaveChangesAsync();
        await using var db1 = PostgresFixture.CreateSibling(db);
        await using var db2 = PostgresFixture.CreateSibling(db);

        var results = await Task.WhenAll(Attempt(Service(db1).RedeemAsync(lan.Id, gift.Id)), Attempt(Service(db2).RedeemAsync(minh.Id, gift.Id)));

        Assert.Equal(1, results.Count(ok => ok));
        Assert.Equal(0, (await db.Gifts.AsNoTracking().SingleAsync()).StockAvailable);
    }

    [Fact]
    public async Task Concurrent_redemptions_cannot_overspend_the_balance()
    {
        await using var db = await fixture.CreateContextAsync();
        var (customer, gift) = await ArrangeAsync(db, balance: 1_000, cost: 800);
        await using var db1 = PostgresFixture.CreateSibling(db);
        await using var db2 = PostgresFixture.CreateSibling(db);

        var results = await Task.WhenAll(Attempt(Service(db1).RedeemAsync(customer.Id, gift.Id)), Attempt(Service(db2).RedeemAsync(customer.Id, gift.Id)));

        Assert.Equal(1, results.Count(ok => ok));
        Assert.Equal(200, (await db.CustomerLoyalty.AsNoTracking().SingleAsync()).Balance);
    }

    [Fact]
    public async Task Overview_shows_points_needed_for_the_next_tier_and_gift_eligibility()
    {
        await using var db = await fixture.CreateContextAsync();
        var (customer, _) = await ArrangeAsync(db, balance: 500); // gift costs 800

        var overview = await Service(db).OverviewAsync(customer.Id);

        Assert.Equal(("Bạc", "Vàng", 15_000L), (overview.TierName, overview.NextTierName, overview.PointsToNextTier)); // 30.000 − 15.000
        Assert.Equal("Bạn không đủ điểm (cần 300 điểm nữa).", overview.Gifts.Single().BlockedReason);
    }

    private static async Task<bool> Attempt(Task task)
    {
        try { await task; return true; }
        catch (DomainException) { return false; }
    }

    [Fact]
    public async Task Missing_points_are_formatted_the_vietnamese_way()
    {
        await using var db = await fixture.CreateContextAsync();
        var (customer, _) = await ArrangeAsync(db, balance: 0, cost: 12_000);

        var overview = await Service(db).OverviewAsync(customer.Id);

        Assert.Equal("Bạn không đủ điểm (cần 12.000 điểm nữa).", overview.Gifts.Single().BlockedReason);
    }
}

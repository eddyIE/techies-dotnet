using Microsoft.EntityFrameworkCore;
using Techies.Web.Data.Seed;
using Techies.Web.Domain;
using Techies.Web.Services;
using Techies.Web.ViewModels.Loyalty;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class TierPolicyTests(PostgresFixture fixture)
{
    private static List<TierInput> Policy(long t1, long t2, long t3) =>
    [
        new() { Tier = 1, Name = "Bạc", ThresholdPoints = t1, DiscountPercent = 10 },
        new() { Tier = 2, Name = "Vàng", ThresholdPoints = t2, DiscountPercent = 30 },
        new() { Tier = 3, Name = "Kim cương", ThresholdPoints = t3, DiscountPercent = 50 },
    ];

    [Fact]
    public async Task Raising_a_threshold_recomputes_customer_tiers()
    {
        await using var db = await fixture.CreateContextAsync();
        await new DbSeeder(db, TimeProvider.System).SeedAsync();
        var loyalty = await db.CustomerLoyalty.FirstAsync();
        loyalty.LifetimePoints = 12_000;
        loyalty.Tier = 1;
        await db.SaveChangesAsync();

        await new LoyaltyService(db, TimeProvider.System).UpdateTiersAsync(Policy(15_000, 30_000, 60_000));

        db.ChangeTracker.Clear();
        Assert.Equal(0, (await db.CustomerLoyalty.SingleAsync(l => l.CustomerId == loyalty.CustomerId)).Tier);
        Assert.Equal(15_000, (await db.MembershipTiers.SingleAsync(t => t.Tier == 1)).ThresholdPoints);
    }

    [Fact]
    public async Task Thresholds_must_strictly_increase()
    {
        await using var db = await fixture.CreateContextAsync();
        await new DbSeeder(db, TimeProvider.System).SeedAsync();

        await Assert.ThrowsAsync<DomainException>(
            () => new LoyaltyService(db, TimeProvider.System).UpdateTiersAsync(Policy(30_000, 30_000, 60_000)));
    }

    [Fact]
    public async Task Discount_percent_must_be_between_0_and_100()
    {
        await using var db = await fixture.CreateContextAsync();
        await new DbSeeder(db, TimeProvider.System).SeedAsync();
        var policy = Policy(10_000, 30_000, 60_000);
        policy[2].DiscountPercent = 101;

        await Assert.ThrowsAsync<DomainException>(() => new LoyaltyService(db, TimeProvider.System).UpdateTiersAsync(policy));
    }
}

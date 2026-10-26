using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Data.Seed;
using Techies.Web.Domain;
using Techies.Web.Services;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class GiftCounterTests(PostgresFixture fixture)
{
    private static async Task<(GiftRedemption Redemption, Staff Staff)> ArrangeAsync(AppDbContext db)
    {
        await new DbSeeder(db, TimeProvider.System).SeedAsync();
        var customer = await db.Users.FirstAsync();
        var gift = await db.Gifts.FirstAsync();
        var staff = await db.Staff.FirstAsync(s => s.Role == StaffRole.OrderStaff);
        var redemption = new GiftRedemption
        {
            Code = "GIFT-G53Y-ZDS3", GiftId = gift.Id, CustomerId = customer.Id, PointsSpent = gift.PointsCost,
            RedeemedAt = DateTimeOffset.UtcNow.AddDays(-1),
        };
        db.GiftRedemptions.Add(redemption);
        await db.SaveChangesAsync();
        return (redemption, staff);
    }

    [Fact]
    public async Task Lookup_is_case_insensitive_and_shows_customer_and_gift()
    {
        await using var db = await fixture.CreateContextAsync();
        var (redemption, _) = await ArrangeAsync(db);

        var found = await new GiftService(db, TimeProvider.System).FindRedemptionAsync(" gift-g53y-zds3 ");

        Assert.NotNull(found);
        Assert.Equal(redemption.Id, found.Id);
        Assert.Equal(GiftRedemptionStatus.Issued, found.Status);
        Assert.False(string.IsNullOrEmpty(found.GiftName));
        Assert.False(string.IsNullOrEmpty(found.CustomerName));
    }

    [Fact]
    public async Task Lookup_of_unknown_code_returns_null()
    {
        await using var db = await fixture.CreateContextAsync();
        await ArrangeAsync(db);

        Assert.Null(await new GiftService(db, TimeProvider.System).FindRedemptionAsync("GIFT-XXXX-XXXX"));
    }

    [Fact]
    public async Task Claim_marks_redemption_claimed_by_staff()
    {
        await using var db = await fixture.CreateContextAsync();
        var (redemption, staff) = await ArrangeAsync(db);

        await new GiftService(db, TimeProvider.System).ClaimAsync(redemption.Code, staff.Id);

        db.ChangeTracker.Clear();
        var claimed = await db.GiftRedemptions.SingleAsync();
        Assert.Equal(GiftRedemptionStatus.Claimed, claimed.Status);
        Assert.Equal(staff.Id, claimed.ClaimedBy);
        Assert.NotNull(claimed.ClaimedAt);
    }

    [Fact]
    public async Task Claiming_twice_is_rejected()
    {
        await using var db = await fixture.CreateContextAsync();
        var (redemption, staff) = await ArrangeAsync(db);
        var service = new GiftService(db, TimeProvider.System);
        await service.ClaimAsync(redemption.Code, staff.Id);

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.ClaimAsync(redemption.Code, staff.Id));
        Assert.Contains("đã được nhận", ex.Message);
    }
}

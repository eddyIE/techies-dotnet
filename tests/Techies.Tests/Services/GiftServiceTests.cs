using Techies.Web.Data.Seed;
using Techies.Web.Domain;
using Techies.Web.Services;
using Techies.Web.ViewModels.Loyalty;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class GiftServiceTests(PostgresFixture fixture)
{
    private static GiftForm Form() => new() { Name = "Ốp lưng", PointsCost = 500, MinTier = 0, StockAvailable = 10, Active = true };

    [Fact]
    public async Task Create_and_update_gift()
    {
        await using var db = await fixture.CreateContextAsync();
        await new DbSeeder(db, TimeProvider.System).SeedAsync();
        var service = new GiftService(db, TimeProvider.System);

        var gift = await service.CreateAsync(Form(), "/uploads/g.png");
        var edit = Form();
        edit.StockAvailable = 0;
        await service.UpdateAsync(gift.Id, edit, imageUrl: null);

        var updated = await service.GetAsync(gift.Id);
        Assert.Equal(0, updated.StockAvailable);
        Assert.Equal("/uploads/g.png", updated.ImageUrl);
    }

    [Theory]
    [InlineData(0, 0, 1)]
    [InlineData(100, 4, 1)]
    [InlineData(100, -1, 1)]
    [InlineData(100, 0, -1)]
    public async Task Create_rejects_invalid_points_tier_or_stock(long points, short minTier, int stock)
    {
        await using var db = await fixture.CreateContextAsync();
        await new DbSeeder(db, TimeProvider.System).SeedAsync();
        var form = Form();
        form.PointsCost = points;
        form.MinTier = minTier;
        form.StockAvailable = stock;

        await Assert.ThrowsAsync<DomainException>(() => new GiftService(db, TimeProvider.System).CreateAsync(form, null));
    }
}

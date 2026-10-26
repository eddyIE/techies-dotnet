using Techies.Web.Services;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class ShopQueryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Listing_never_shows_inactive_products()
    {
        await using var db = await fixture.CreateContextAsync();
        var phones = TestData.Category(db);
        TestData.Product(db, phones, "Đang bán");
        TestData.Product(db, phones, "Ngừng bán", active: false);
        await db.SaveChangesAsync();

        var page = await new ShopQueries(db).ListAsync(categorySlug: null, q: null, page: 1);

        Assert.Equal(["Đang bán"], page.Items.Select(p => p.Name));
    }

    [Fact]
    public async Task Detail_of_inactive_product_is_not_found()
    {
        await using var db = await fixture.CreateContextAsync();
        var hidden = TestData.Product(db, TestData.Category(db), "Ngừng bán", active: false);
        await db.SaveChangesAsync();

        Assert.Null(await new ShopQueries(db).DetailAsync(hidden.Slug));
    }

    [Fact]
    public async Task Detail_includes_average_rating_of_reviews()
    {
        await using var db = await fixture.CreateContextAsync();
        TestData.Tiers(db);
        var customer = TestData.Customer(db);
        var product = TestData.Product(db, TestData.Category(db), "Loa");
        // One review per order line, so each review comes from its own order.
        db.Reviews.AddRange(
            new Web.Domain.Review { Customer = customer, Product = product, OrderId = TestData.Order(db, customer, product).Id, Rating = 5, CreatedAt = DateTimeOffset.UtcNow },
            new Web.Domain.Review { Customer = customer, Product = product, OrderId = TestData.Order(db, customer, product).Id, Rating = 2, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var detail = await new ShopQueries(db).DetailAsync(product.Slug);

        Assert.Equal(3.5, detail!.AverageRating);
        Assert.Equal(2, detail.Reviews.Count);
    }
}

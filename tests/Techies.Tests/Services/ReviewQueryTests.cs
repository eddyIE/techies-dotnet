using Techies.Web.Domain;
using Techies.Web.Services;
using Techies.Web.ViewModels.Customers;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class ReviewQueryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Low_rating_filter_returns_only_one_and_two_stars_newest_first()
    {
        await using var db = await fixture.CreateContextAsync();
        TestData.Tiers(db);
        var customer = TestData.Customer(db);
        var product = TestData.Product(db, TestData.Category(db), "Loa");
        var order = TestData.Order(db, customer, product);
        var now = DateTimeOffset.UtcNow;
        foreach (var (rating, daysAgo) in new (short, int)[] { (1, 3), (2, 1), (3, 2), (5, 0) })
            db.Reviews.Add(new Review { Customer = customer, Product = product, OrderId = order.Id, Rating = rating, Comment = $"{rating} sao", CreatedAt = now.AddDays(-daysAgo) });
        await db.SaveChangesAsync();

        var page = await new ReviewQueries(db).ListAsync(new ReviewFilter { LowOnly = true });

        Assert.Equal(["2 sao", "1 sao"], page.Items.Select(r => r.Comment));
    }

    [Fact]
    public async Task Rating_and_product_filters_combine()
    {
        await using var db = await fixture.CreateContextAsync();
        TestData.Tiers(db);
        var customer = TestData.Customer(db);
        var category = TestData.Category(db);
        var speaker = TestData.Product(db, category, "Loa");
        var phone = TestData.Product(db, category, "Điện thoại");
        var order = TestData.Order(db, customer, speaker);
        db.Reviews.AddRange(
            new Review { Customer = customer, Product = speaker, OrderId = order.Id, Rating = 4, CreatedAt = DateTimeOffset.UtcNow },
            new Review { Customer = customer, Product = phone, OrderId = order.Id, Rating = 4, CreatedAt = DateTimeOffset.UtcNow },
            new Review { Customer = customer, Product = speaker, OrderId = order.Id, Rating = 5, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var page = await new ReviewQueries(db).ListAsync(new ReviewFilter { Rating = 4, ProductId = speaker.Id });

        Assert.Single(page.Items);
    }
}

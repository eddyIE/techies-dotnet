using Microsoft.AspNetCore.Http;
using Techies.Web.Domain;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.Services;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class ReviewQueryTests(PostgresFixture fixture)
{
    private static GridRequest Request(string query) =>
        GridRequest.From("/admin/reviews", new QueryString(query), ReviewQueries.GridSorts, "date", true);

    [Fact]
    public async Task Rating_range_1_to_2_returns_low_ratings_newest_first()
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

        var (rows, _) = await new ReviewQueries(db).GridAsync(Request("?rating_from=1&rating_to=2"));

        Assert.Equal(["2 sao", "1 sao"], rows.Select(r => r.Comment));
    }

    [Fact]
    public async Task Product_filter_and_comment_keyword_combine()
    {
        await using var db = await fixture.CreateContextAsync();
        TestData.Tiers(db);
        var customer = TestData.Customer(db);
        var category = TestData.Category(db);
        var speaker = TestData.Product(db, category, "Loa");
        var phone = TestData.Product(db, category, "Điện thoại");
        var order = TestData.Order(db, customer, speaker);
        db.Reviews.AddRange(
            new Review { Customer = customer, Product = speaker, OrderId = order.Id, Rating = 4, Comment = "Pin yếu", CreatedAt = DateTimeOffset.UtcNow },
            new Review { Customer = customer, Product = phone, OrderId = order.Id, Rating = 4, Comment = "Pin yếu", CreatedAt = DateTimeOffset.UtcNow },
            new Review { Customer = customer, Product = speaker, OrderId = order.Id, Rating = 5, Comment = "Âm thanh hay", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var (rows, _) = await new ReviewQueries(db).GridAsync(Request($"?product={speaker.Id}&q=pin"));

        Assert.Single(rows);
    }
}

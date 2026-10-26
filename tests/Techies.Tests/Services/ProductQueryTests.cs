using Microsoft.AspNetCore.Http;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.Services;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class ProductQueryTests(PostgresFixture fixture)
{
    private static GridRequest Request(string query) =>
        GridRequest.From("/admin/products", new QueryString(query), ProductService.GridSorts, "name", false);

    [Fact]
    public async Task Keyword_matches_name_case_insensitively()
    {
        await using var db = await fixture.CreateContextAsync();
        var phones = TestData.Category(db);
        TestData.Product(db, phones, "iPhone 15 Pro");
        TestData.Product(db, phones, "Samsung Galaxy S24");
        await db.SaveChangesAsync();

        var (rows, _) = await new ProductService(db).GridAsync(Request("?q=IPHONE"));

        Assert.Equal(["iPhone 15 Pro"], rows.Select(p => p.Name));
    }

    [Fact]
    public async Task Category_out_of_stock_and_active_filters_combine()
    {
        await using var db = await fixture.CreateContextAsync();
        var phones = TestData.Category(db);
        var laptops = TestData.Category(db, "Laptop");
        TestData.Product(db, phones, "Hết hàng, đang bán", available: 0);
        TestData.Product(db, phones, "Hết hàng, ngừng bán", available: 0, active: false);
        TestData.Product(db, phones, "Còn hàng", available: 5);
        TestData.Product(db, laptops, "Laptop hết hàng", available: 0);
        await db.SaveChangesAsync();

        var (rows, _) = await new ProductService(db).GridAsync(Request($"?category={phones.Id}&stock_to=0&active=true"));

        Assert.Equal(["Hết hàng, đang bán"], rows.Select(p => p.Name));
    }

    [Fact]
    public async Task Unknown_sort_key_falls_back_to_name()
    {
        await using var db = await fixture.CreateContextAsync();
        var phones = TestData.Category(db);
        TestData.Product(db, phones, "B", price: 1);
        TestData.Product(db, phones, "A", price: 2);
        await db.SaveChangesAsync();

        var (rows, _) = await new ProductService(db).GridAsync(Request("?sort=password_hash&dir=desc"));

        Assert.Equal(["A", "B"], rows.Select(p => p.Name));
    }
}

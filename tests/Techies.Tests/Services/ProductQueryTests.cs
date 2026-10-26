using Techies.Web.Services;
using Techies.Web.ViewModels.Catalog;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class ProductQueryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Search_matches_name_case_insensitively()
    {
        await using var db = await fixture.CreateContextAsync();
        var phones = TestData.Category(db);
        TestData.Product(db, phones, "iPhone 15 Pro");
        TestData.Product(db, phones, "Samsung Galaxy S24");
        await db.SaveChangesAsync();

        var page = await new ProductService(db).SearchAsync(new ProductFilter { Q = "IPHONE" });

        Assert.Equal(["iPhone 15 Pro"], page.Items.Select(p => p.Name));
    }

    [Fact]
    public async Task Filters_combine_category_out_of_stock_and_active()
    {
        await using var db = await fixture.CreateContextAsync();
        var phones = TestData.Category(db);
        var laptops = TestData.Category(db, "Laptop");
        TestData.Product(db, phones, "Hết hàng, đang bán", available: 0);
        TestData.Product(db, phones, "Hết hàng, ngừng bán", available: 0, active: false);
        TestData.Product(db, phones, "Còn hàng", available: 5);
        TestData.Product(db, laptops, "Laptop hết hàng", available: 0);
        await db.SaveChangesAsync();

        var page = await new ProductService(db).SearchAsync(
            new ProductFilter { CategoryId = phones.Id, OutOfStock = true, Active = true });

        Assert.Equal(["Hết hàng, đang bán"], page.Items.Select(p => p.Name));
    }

    [Fact]
    public async Task Results_are_paged_20_per_page()
    {
        await using var db = await fixture.CreateContextAsync();
        var phones = TestData.Category(db);
        for (var i = 1; i <= 25; i++) TestData.Product(db, phones, $"Máy {i:00}");
        await db.SaveChangesAsync();

        var page2 = await new ProductService(db).SearchAsync(new ProductFilter { Page = 2 });

        Assert.Equal(25, page2.Total);
        Assert.Equal(5, page2.Items.Count);
        Assert.Equal(2, page2.TotalPages);
    }
}

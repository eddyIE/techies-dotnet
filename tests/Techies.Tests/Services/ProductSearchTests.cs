using Microsoft.AspNetCore.Http;
using Techies.Web.Data;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.Services;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class ProductSearchTests(PostgresFixture fixture)
{
    private static async Task ArrangeAsync(AppDbContext db)
    {
        var phones = TestData.Category(db, "Điện thoại");
        var laptops = TestData.Category(db, "Laptop");
        TestData.Product(db, phones, "Samsung Galaxy S24");
        TestData.Product(db, phones, "iPhone 15");
        TestData.Product(db, laptops, "Samsung Galaxy Book4");
        TestData.Product(db, laptops, "Tai nghe đi kèm laptop");
        await db.SaveChangesAsync();
    }

    [Theory]
    [InlineData("điện thoại")]
    [InlineData("dien thoai")]
    [InlineData("ĐIỆN THOẠI")]
    public async Task Keyword_matching_a_category_lists_its_products_regardless_of_accents(string q)
    {
        await using var db = await fixture.CreateContextAsync();
        await ArrangeAsync(db);

        var page = await new ShopQueries(db).ListAsync(null, q, 1);

        Assert.Equal(["Samsung Galaxy S24", "iPhone 15"], page.Items.Select(p => p.Name).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Every_word_must_match_name_or_category()
    {
        await using var db = await fixture.CreateContextAsync();
        await ArrangeAsync(db);

        var page = await new ShopQueries(db).ListAsync(null, "dien thoai samsung", 1);

        Assert.Equal(["Samsung Galaxy S24"], page.Items.Select(p => p.Name));
    }

    [Fact]
    public async Task Name_matches_still_work_across_categories()
    {
        await using var db = await fixture.CreateContextAsync();
        await ArrangeAsync(db);

        var page = await new ShopQueries(db).ListAsync(null, "galaxy", 1);

        Assert.Equal(["Samsung Galaxy Book4", "Samsung Galaxy S24"], page.Items.Select(p => p.Name).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Admin_product_grid_uses_the_same_search()
    {
        await using var db = await fixture.CreateContextAsync();
        await ArrangeAsync(db);
        var request = GridRequest.From("/admin/products", new QueryString("?q=tai+nghe"), ProductService.GridSorts, "name", false);

        var (rows, _) = await new ProductService(db).GridAsync(request);

        Assert.Equal(["Tai nghe đi kèm laptop"], rows.Select(r => r.Name));
    }
}

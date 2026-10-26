using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.Services;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class ProductGridTests(PostgresFixture fixture)
{
    private static GridRequest Request(string query) =>
        GridRequest.From("/admin/products", new QueryString(query), ProductService.GridSorts, "name", false);

    [Fact]
    public async Task Price_and_stock_ranges_combine_with_active_filter_and_sort()
    {
        await using var db = await fixture.CreateContextAsync();
        var category = TestData.Category(db);
        TestData.Product(db, category, "A rẻ", price: 100_000, available: 3);
        TestData.Product(db, category, "B vừa", price: 500_000, available: 0);
        TestData.Product(db, category, "C vừa ngừng bán", price: 600_000, available: 2, active: false);
        TestData.Product(db, category, "D đắt", price: 900_000, available: 1);
        await db.SaveChangesAsync();

        var (rows, total) = await new ProductService(db).GridAsync(Request("?price_from=200000&stock_to=2&active=true&sort=price&dir=desc"));

        Assert.Equal(2, total);
        Assert.Equal(["D đắt", "B vừa"], rows.Select(r => r.Name));
    }

    [Fact]
    public async Task Paging_uses_the_requested_page_size()
    {
        await using var db = await fixture.CreateContextAsync();
        var category = TestData.Category(db);
        for (var i = 1; i <= 60; i++) TestData.Product(db, category, $"Máy {i:00}");
        await db.SaveChangesAsync();

        var (rows, total) = await new ProductService(db).GridAsync(Request("?size=50&page=2"));

        Assert.Equal((60, 10), (total, rows.Count));
    }

    [Fact]
    public async Task Bulk_toggle_switches_every_selected_product()
    {
        await using var db = await fixture.CreateContextAsync();
        var category = TestData.Category(db);
        var a = TestData.Product(db, category, "A");
        var b = TestData.Product(db, category, "B");
        await db.SaveChangesAsync();
        var service = new ProductService(db);

        var result = await Bulk.RunAsync([a.Id, b.Id, Guid.NewGuid()], id => service.SetActiveAsync(id, false));

        Assert.Equal((2, 3), (result.Done, result.Total));
        Assert.Equal("Đã tắt kinh doanh 2 / 3. Bỏ qua: Không tìm thấy sản phẩm.", result.Summary("tắt kinh doanh"));
        db.ChangeTracker.Clear();
        Assert.False(await db.Products.AnyAsync(p => p.Active));
    }
}

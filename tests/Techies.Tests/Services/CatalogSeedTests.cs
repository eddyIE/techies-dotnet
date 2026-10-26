using Microsoft.EntityFrameworkCore;
using Techies.Web.Data.Seed;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class CatalogSeedTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Seed_loads_catalog_ported_from_spring_project()
    {
        await using var db = await fixture.CreateContextAsync();

        await new DbSeeder(db, TimeProvider.System).SeedAsync();

        // Counts of the final ../techies catalog state, minus its hidden "Demo" category.
        Assert.Equal(11, await db.Categories.CountAsync());
        Assert.Equal(62, await db.Products.CountAsync());
        Assert.Equal(183, await db.ProductSpecs.CountAsync());
        Assert.Equal(104, await db.ProductImages.CountAsync());
        Assert.Equal(62, await db.StockItems.CountAsync());
    }

    [Fact]
    public async Task Seeded_stock_includes_out_of_stock_and_low_stock_items()
    {
        await using var db = await fixture.CreateContextAsync();

        await new DbSeeder(db, TimeProvider.System).SeedAsync();

        Assert.True(await db.StockItems.CountAsync(s => s.Available == 0) >= 1);
        Assert.True(await db.StockItems.CountAsync(s => s.Available > 0 && s.Available <= 5) >= 1);
    }
}

using Microsoft.EntityFrameworkCore;
using Techies.Web.Domain;
using Techies.Web.Services;
using Techies.Web.ViewModels.Catalog;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class ProductEditTests(PostgresFixture fixture)
{
    private static ProductForm Form(Guid categoryId) => new()
    {
        Name = "Tivi Sony 75 inch", CategoryId = categoryId, Price = 25_000_000, Description = "Tivi 4K", Active = true,
        Specs = [new() { Name = "Kích cỡ màn hình", Value = "75 inch" }, new() { Name = "Độ phân giải", Value = "4K" }],
    };

    [Fact]
    public async Task Create_saves_specs_images_and_zero_stock()
    {
        await using var db = await fixture.CreateContextAsync();
        var tv = TestData.Category(db, "Tivi");
        await db.SaveChangesAsync();

        var id = await new ProductService(db).CreateAsync(Form(tv.Id), "/uploads/t.png", ["/uploads/1.png", "/uploads/2.png"]);

        var product = await db.Products.Include(p => p.Specs).Include(p => p.Images).Include(p => p.Stock).SingleAsync(p => p.Id == id);
        Assert.Equal("tivi-sony-75-inch", product.Slug);
        Assert.Equal("/uploads/t.png", product.ThumbnailUrl);
        Assert.Equal(["Kích cỡ màn hình", "Độ phân giải"], product.Specs.OrderBy(s => s.DisplayOrder).Select(s => s.SpecName));
        Assert.Equal(["/uploads/1.png", "/uploads/2.png"], product.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url));
        Assert.Equal(0, product.Stock!.Available);
    }

    [Fact]
    public async Task Update_replaces_specs_and_images_in_submitted_order()
    {
        await using var db = await fixture.CreateContextAsync();
        var tv = TestData.Category(db, "Tivi");
        await db.SaveChangesAsync();
        var service = new ProductService(db);
        var id = await service.CreateAsync(Form(tv.Id), null, ["/uploads/1.png", "/uploads/2.png", "/uploads/3.png"]);

        var edit = Form(tv.Id);
        edit.Specs = [new() { Name = "Độ phân giải", Value = "8K" }, new() { Name = " ", Value = "" }];
        edit.ExistingImages = ["/uploads/3.png", "/uploads/1.png"];
        await service.UpdateAsync(id, edit, thumbnailUrl: null, newImageUrls: ["/uploads/4.png"]);

        db.ChangeTracker.Clear();
        var product = await db.Products.Include(p => p.Specs).Include(p => p.Images).SingleAsync(p => p.Id == id);
        Assert.Equal(["Độ phân giải: 8K"], product.Specs.Select(s => $"{s.SpecName}: {s.SpecValue}"));
        Assert.Equal(["/uploads/3.png", "/uploads/1.png", "/uploads/4.png"], product.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url));
    }

    [Fact]
    public async Task Create_rejects_non_positive_price()
    {
        await using var db = await fixture.CreateContextAsync();
        var tv = TestData.Category(db, "Tivi");
        await db.SaveChangesAsync();
        var form = Form(tv.Id);
        form.Price = 0;

        await Assert.ThrowsAsync<DomainException>(() => new ProductService(db).CreateAsync(form, null, []));
    }
}

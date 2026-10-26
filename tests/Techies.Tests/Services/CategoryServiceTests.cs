using Techies.Web.Domain;
using Techies.Web.Services;
using Techies.Web.ViewModels.Catalog;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class CategoryServiceTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Create_generates_slug_from_vietnamese_name()
    {
        await using var db = await fixture.CreateContextAsync();
        var service = new CategoryService(db);

        var category = await service.CreateAsync(new CategoryForm { Name = "Điện thoại", DisplayOrder = 1 }, iconUrl: null);

        Assert.Equal("dien-thoai", category.Slug);
    }

    [Fact]
    public async Task Create_keeps_slug_unique_with_numeric_suffix()
    {
        await using var db = await fixture.CreateContextAsync();
        var service = new CategoryService(db);
        await service.CreateAsync(new CategoryForm { Name = "Phụ kiện" }, null);

        var second = await service.CreateAsync(new CategoryForm { Name = "Phụ  kiện" }, null);

        Assert.Equal("phu-kien-2", second.Slug);
    }

    [Fact]
    public async Task Update_keeps_existing_icon_when_no_new_upload()
    {
        await using var db = await fixture.CreateContextAsync();
        var service = new CategoryService(db);
        var category = await service.CreateAsync(new CategoryForm { Name = "Laptop" }, "/uploads/2026/a.png");

        await service.UpdateAsync(category.Id, new CategoryForm { Name = "Laptop gaming", DisplayOrder = 3 }, iconUrl: null);

        var updated = await service.GetAsync(category.Id);
        Assert.Equal("Laptop gaming", updated.Name);
        Assert.Equal("/uploads/2026/a.png", updated.ImageUrl);
        Assert.Equal(3, updated.DisplayOrder);
    }

    [Fact]
    public async Task Get_unknown_category_throws()
    {
        await using var db = await fixture.CreateContextAsync();

        await Assert.ThrowsAsync<DomainException>(() => new CategoryService(db).GetAsync(Guid.NewGuid()));
    }
}

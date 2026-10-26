using Techies.Web.Data;
using Techies.Web.Domain;

namespace Techies.Tests;

// Small builders for service tests. Each test still states the values it asserts on.
public static class TestData
{
    public static Category Category(AppDbContext db, string name = "Điện thoại")
    {
        var category = new Category { Name = name, Slug = Guid.NewGuid().ToString("N") };
        db.Categories.Add(category);
        return category;
    }

    public static Product Product(AppDbContext db, Category category, string name, decimal price = 1_000_000,
        int available = 10, bool active = true)
    {
        var product = new Product
        {
            Category = category, Name = name, Slug = Guid.NewGuid().ToString("N"), Description = "Mô tả",
            Price = price, Active = active, CreatedAt = DateTimeOffset.UtcNow,
            Stock = new StockItem { Available = available, UpdatedAt = DateTimeOffset.UtcNow },
        };
        db.Products.Add(product);
        return product;
    }
}

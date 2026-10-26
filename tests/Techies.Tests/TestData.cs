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

    public static void Tiers(AppDbContext db) => db.MembershipTiers.AddRange(
        new MembershipTier { Tier = 0, Name = "Thành viên", ThresholdPoints = 0 },
        new MembershipTier { Tier = 1, Name = "Bạc", ThresholdPoints = 10_000, DiscountPercent = 10 },
        new MembershipTier { Tier = 2, Name = "Vàng", ThresholdPoints = 30_000, DiscountPercent = 30 },
        new MembershipTier { Tier = 3, Name = "Kim cương", ThresholdPoints = 60_000, DiscountPercent = 50 });

    public static User Customer(AppDbContext db, string email = "khach@example.com", long lifetimePoints = 0)
    {
        var user = new User
        {
            Email = email, PasswordHash = "x", FullName = "Khách Hàng", Phone = "0900000001",
            Loyalty = new CustomerLoyalty { LifetimePoints = lifetimePoints, Balance = lifetimePoints, Tier = 0 },
        };
        db.Users.Add(user);
        return user;
    }

    // A CONFIRMED order for `quantity` units of `product`; stock is assumed already reserved.
    public static Order Order(AppDbContext db, User customer, Product product, int quantity = 2,
        PaymentMethod method = PaymentMethod.Cod, PaymentStatus payment = PaymentStatus.Pending,
        OrderStatus status = OrderStatus.Confirmed, decimal discount = 0, string? couponCode = null)
    {
        var subtotal = product.Price * quantity;
        var shipping = Web.Services.OrderMath.ShippingFee(subtotal);
        var order = new Order
        {
            User = customer, Status = status, PaymentMethod = method, PaymentStatus = payment,
            Subtotal = subtotal, ShippingFee = shipping, Discount = discount, CouponCode = couponCode,
            Total = Web.Services.OrderMath.Total(subtotal, shipping, discount),
            ShipRecipient = customer.FullName, ShipPhone = customer.Phone, ShipLine1 = "1 Lê Lợi", ShipWard = "Bến Nghé",
            ShipDistrict = "Quận 1", ShipProvince = "TP. Hồ Chí Minh", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow,
            Items = [new OrderItem { ProductId = product.Id, ProductName = product.Name, UnitPrice = product.Price, Quantity = quantity, LineTotal = subtotal }],
        };
        db.Orders.Add(order);
        return order;
    }
}

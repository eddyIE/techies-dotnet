namespace Techies.Web.Domain;

public sealed class Category
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
}

public sealed class Product
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool Active { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }

    public List<ProductImage> Images { get; set; } = [];
    public List<ProductSpec> Specs { get; set; } = [];
    public StockItem? Stock { get; set; }
}

public sealed class ProductImage
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ProductId { get; set; }
    public string Url { get; set; } = "";
    public int DisplayOrder { get; set; }
}

public sealed class ProductSpec
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ProductId { get; set; }
    public string SpecName { get; set; } = "";
    public string SpecValue { get; set; } = "";
    public int DisplayOrder { get; set; }
}

public sealed class StockItem
{
    public Guid ProductId { get; set; }
    public int Available { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public enum AdjustmentType { Import, Count, Order, Cancel }

// Audit trail of every stock change: staff imports/counts, checkout reservations, cancellations.
public sealed class InventoryAdjustment
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ProductId { get; set; }
    public AdjustmentType AdjustmentType { get; set; }
    public int QuantityDelta { get; set; }
    public int QuantityBefore { get; set; }
    public int QuantityAfter { get; set; }
    public string? Reason { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? StaffId { get; set; }
    public Staff? Staff { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

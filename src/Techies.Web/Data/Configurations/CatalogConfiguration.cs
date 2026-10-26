using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Techies.Web.Domain;

namespace Techies.Web.Data.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.Property(c => c.Name).HasMaxLength(120);
        b.Property(c => c.Slug).HasMaxLength(140);
        b.HasIndex(c => c.Slug).IsUnique();
        b.Property(c => c.ImageUrl).HasMaxLength(500);
    }
}

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable(t => t.HasCheckConstraint("ck_products_price", "price > 0"));
        b.Property(p => p.Name).HasMaxLength(200);
        b.Property(p => p.Slug).HasMaxLength(220);
        b.HasIndex(p => p.Slug).IsUnique();
        b.Property(p => p.ThumbnailUrl).HasMaxLength(500);
        b.HasIndex(p => p.Active);
        b.HasOne(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(p => p.Images).WithOne().HasForeignKey(i => i.ProductId);
        b.HasMany(p => p.Specs).WithOne().HasForeignKey(s => s.ProductId);
        b.HasOne(p => p.Stock).WithOne().HasForeignKey<StockItem>(s => s.ProductId);
    }
}

internal sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> b) => b.Property(i => i.Url).HasMaxLength(500);
}

internal sealed class ProductSpecConfiguration : IEntityTypeConfiguration<ProductSpec>
{
    public void Configure(EntityTypeBuilder<ProductSpec> b)
    {
        b.Property(s => s.SpecName).HasMaxLength(120);
        b.Property(s => s.SpecValue).HasMaxLength(500);
    }
}

internal sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> b)
    {
        b.HasKey(s => s.ProductId);
        // Last line of defence against overselling: checkout's conditional update relies on it.
        b.ToTable(t => t.HasCheckConstraint("ck_stock_items_available", "available >= 0"));
    }
}

internal sealed class InventoryAdjustmentConfiguration : IEntityTypeConfiguration<InventoryAdjustment>
{
    public void Configure(EntityTypeBuilder<InventoryAdjustment> b)
    {
        b.Property(a => a.AdjustmentType).HasMaxLength(16);
        b.HasOne<Product>().WithMany().HasForeignKey(a => a.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(a => a.Staff).WithMany().HasForeignKey(a => a.StaffId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(a => new { a.ProductId, a.CreatedAt });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Techies.Web.Domain;

namespace Techies.Web.Data.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public const string RefSequence = "order_ref_seq";

    public void Configure(EntityTypeBuilder<Order> b)
    {
        // ORD-100001, ORD-100002, ... from one global sequence (as in ../techies V9).
        b.Property(o => o.OrderRef).HasMaxLength(20)
            .HasDefaultValueSql($"'ORD-' || nextval('{RefSequence}')").ValueGeneratedOnAdd();
        b.HasIndex(o => o.OrderRef).IsUnique();
        b.HasIndex(o => o.Status);
        b.HasIndex(o => new { o.UserId, o.CreatedAt });
        b.HasIndex(o => o.CompletedAt);
        b.HasIndex(o => o.ShipPhone);
        b.Property(o => o.Status).HasMaxLength(16);
        b.Property(o => o.FailureCode).HasMaxLength(32);
        b.Property(o => o.CouponCode).HasMaxLength(40);
        b.Property(o => o.PaymentMethod).HasMaxLength(16);
        b.Property(o => o.PaymentStatus).HasMaxLength(16);
        b.Property(o => o.ShipRecipient).HasMaxLength(120);
        b.Property(o => o.ShipPhone).HasMaxLength(11);
        b.Property(o => o.ShipLine1).HasMaxLength(255);
        b.Property(o => o.ShipWard).HasMaxLength(120);
        b.Property(o => o.ShipDistrict).HasMaxLength(120);
        b.Property(o => o.ShipProvince).HasMaxLength(120);
        b.HasOne(o => o.User).WithMany().HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId);
    }
}

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.Property(i => i.ProductName).HasMaxLength(200);
        b.Property(i => i.ThumbnailUrl).HasMaxLength(500);
        b.HasOne<Product>().WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t => t.HasCheckConstraint("ck_order_items_quantity", "quantity >= 1"));
    }
}

internal sealed class CouponRedemptionConfiguration : IEntityTypeConfiguration<CouponRedemption>
{
    public void Configure(EntityTypeBuilder<CouponRedemption> b)
    {
        b.Property(r => r.Status).HasMaxLength(16);
        b.HasOne<Coupon>().WithMany().HasForeignKey(r => r.CouponId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Order>().WithMany().HasForeignKey(r => r.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PointTransactionConfiguration : IEntityTypeConfiguration<PointTransaction>
{
    public void Configure(EntityTypeBuilder<PointTransaction> b)
    {
        b.Property(t => t.TransactionType).HasMaxLength(16);
        b.HasOne<User>().WithMany().HasForeignKey(t => t.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Order>().WithMany().HasForeignKey(t => t.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<GiftRedemption>().WithMany().HasForeignKey(t => t.GiftRedemptionId).OnDelete(DeleteBehavior.Restrict);
        // Points are earned at most once per order.
        b.HasIndex(t => t.OrderId).IsUnique().HasFilter("transaction_type = 'EARN'");
        b.HasIndex(t => new { t.CustomerId, t.CreatedAt });
    }
}

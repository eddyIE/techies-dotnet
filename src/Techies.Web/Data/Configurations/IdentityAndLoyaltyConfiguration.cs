using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Techies.Web.Domain;

namespace Techies.Web.Data.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.Property(u => u.Email).HasMaxLength(255);
        b.HasIndex(u => u.Email).IsUnique();
        b.Property(u => u.PasswordHash).HasMaxLength(72);
        b.Property(u => u.FullName).HasMaxLength(120);
        b.Property(u => u.Phone).HasMaxLength(11);
        b.Property(u => u.Status).HasMaxLength(16);
        b.HasMany(u => u.Addresses).WithOne().HasForeignKey(a => a.UserId);
        b.HasOne(u => u.Loyalty).WithOne().HasForeignKey<CustomerLoyalty>(l => l.CustomerId);
    }
}

internal sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> b)
    {
        b.Property(a => a.RecipientName).HasMaxLength(120);
        b.Property(a => a.Phone).HasMaxLength(11);
        b.Property(a => a.Line1).HasMaxLength(255);
        b.Property(a => a.Ward).HasMaxLength(120);
        b.Property(a => a.District).HasMaxLength(120);
        b.Property(a => a.Province).HasMaxLength(120);
    }
}

internal sealed class StaffConfiguration : IEntityTypeConfiguration<Staff>
{
    public void Configure(EntityTypeBuilder<Staff> b)
    {
        b.ToTable("staff");
        b.Property(s => s.FullName).HasMaxLength(120);
        b.Property(s => s.Email).HasMaxLength(255);
        b.HasIndex(s => s.Email).IsUnique();
        b.Property(s => s.Phone).HasMaxLength(11);
        b.HasOne(s => s.Role).WithMany().HasForeignKey(s => s.RoleId).OnDelete(DeleteBehavior.Restrict);
        b.Property(s => s.Status).HasMaxLength(16);
    }
}

internal sealed class MembershipTierConfiguration : IEntityTypeConfiguration<MembershipTier>
{
    public void Configure(EntityTypeBuilder<MembershipTier> b)
    {
        b.HasKey(t => t.Tier);
        b.Property(t => t.Tier).ValueGeneratedNever();
        b.Property(t => t.Name).HasMaxLength(40);
        b.Property(t => t.DiscountPercent).HasPrecision(5, 2);
    }
}

internal sealed class CustomerLoyaltyConfiguration : IEntityTypeConfiguration<CustomerLoyalty>
{
    public void Configure(EntityTypeBuilder<CustomerLoyalty> b)
    {
        b.ToTable("customer_loyalty");
        b.HasKey(l => l.CustomerId);
        b.HasOne<MembershipTier>().WithMany().HasForeignKey(l => l.Tier);
    }
}

internal sealed class GiftConfiguration : IEntityTypeConfiguration<Gift>
{
    public void Configure(EntityTypeBuilder<Gift> b)
    {
        b.Property(g => g.Name).HasMaxLength(160);
        b.Property(g => g.ImageUrl).HasMaxLength(500);
        b.HasOne<MembershipTier>().WithMany().HasForeignKey(g => g.MinTier).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_gifts_points_cost", "points_cost > 0");
            t.HasCheckConstraint("ck_gifts_stock_available", "stock_available >= 0");
        });
    }
}

internal sealed class GiftRedemptionConfiguration : IEntityTypeConfiguration<GiftRedemption>
{
    public void Configure(EntityTypeBuilder<GiftRedemption> b)
    {
        b.Property(r => r.Code).HasMaxLength(32);
        b.HasIndex(r => r.Code).IsUnique();
        b.Property(r => r.Status).HasMaxLength(16);
        b.HasOne(r => r.Gift).WithMany().HasForeignKey(r => r.GiftId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Customer).WithMany().HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.ClaimedByStaff).WithMany().HasForeignKey(r => r.ClaimedBy).OnDelete(DeleteBehavior.Restrict);
    }
}

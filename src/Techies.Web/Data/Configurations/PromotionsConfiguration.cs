using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Techies.Web.Domain;

namespace Techies.Web.Data.Configurations;

internal sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> b)
    {
        b.Property(c => c.Code).HasMaxLength(40);
        b.HasIndex(c => c.Code).IsUnique();
        b.HasOne<Staff>().WithMany().HasForeignKey(c => c.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_coupons_discount_amount", "discount_amount > 0");
            t.HasCheckConstraint("ck_coupons_min_subtotal", "min_subtotal >= 0");
        });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;

namespace Techies.Web.Data.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.Property(r => r.Code).HasMaxLength(32);
        b.HasIndex(r => r.Code).IsUnique();
        b.Property(r => r.Name).HasMaxLength(100);
        b.HasIndex(r => r.Name).IsUnique();
        b.HasMany(r => r.Permissions).WithOne().HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Cascade);
        b.HasData(
            new Role
            {
                Id = SeededRoles.SuperAdmin, Code = "SUPER_ADMIN", Name = "Quản trị viên cao cấp", IsSystem = true,
                Description = "Toàn quyền, quản lý nhân sự và phân quyền."
            },
            new Role
            {
                Id = SeededRoles.CatalogStaff, Code = "CATALOG_STAFF", Name = "Nhân viên Kho & Sản phẩm",
                Description = "Thêm/sửa sản phẩm, thông số, nhập hàng và kiểm kê."
            },
            new Role
            {
                Id = SeededRoles.OrderStaff, Code = "ORDER_STAFF", Name = "Nhân viên Điều phối & Quầy",
                Description = "Xử lý đơn hàng, in phiếu giao, tra cứu quà tặng, hỗ trợ khách hàng."
            });
    }
}

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> b)
    {
        b.Property(p => p.Id).ValueGeneratedNever();
        b.Property(p => p.Code).HasMaxLength(64);
        b.HasIndex(p => p.Code).IsUnique();
        b.Property(p => p.Name).HasMaxLength(150);
        b.Property(p => p.Module).HasMaxLength(50);
        b.HasData(Permissions.All.Select(p => new Permission { Id = p.Id, Code = p.Code, Name = p.Name, Module = p.Module }));
    }
}

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {
        b.HasKey(rp => new { rp.RoleId, rp.PermissionId });
        b.HasOne<Permission>().WithMany().HasForeignKey(rp => rp.PermissionId);
        // Starting permissions of the two editable seeded roles; the system role needs none.
        b.HasData(
            Permissions.CatalogStaffDefaults.Select(c => new RolePermission { RoleId = SeededRoles.CatalogStaff, PermissionId = Permissions.Find(c).Id })
                .Concat(Permissions.OrderStaffDefaults.Select(c => new RolePermission { RoleId = SeededRoles.OrderStaff, PermissionId = Permissions.Find(c).Id })));
    }
}

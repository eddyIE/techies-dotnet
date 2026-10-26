using Microsoft.EntityFrameworkCore;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;

namespace Techies.Web.Data.Seed;

// Runs on every startup; each step skips itself when its data already exists.
public sealed class DbSeeder(AppDbContext db, TimeProvider clock)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedTiersAsync(ct);
        await SeedStaffAsync(ct);
        await CustomerSeed.SeedAsync(db, clock.GetUtcNow(), ct);
        await CatalogSeed.SeedAsync(db, clock.GetUtcNow(), ct);
        await CouponSeed.SeedAsync(db, clock.GetUtcNow(), ct);
        await GiftSeed.SeedAsync(db, clock.GetUtcNow(), ct);
    }

    private async Task SeedTiersAsync(CancellationToken ct)
    {
        if (await db.MembershipTiers.AnyAsync(ct)) return;
        // Thresholds and percentages from ../techies loyalty-service V1__init.sql.
        db.MembershipTiers.AddRange(
            new MembershipTier { Tier = 0, Name = "Thành viên", ThresholdPoints = 0, DiscountPercent = 0 },
            new MembershipTier { Tier = 1, Name = "Bạc", ThresholdPoints = 10_000, DiscountPercent = 10 },
            new MembershipTier { Tier = 2, Name = "Vàng", ThresholdPoints = 30_000, DiscountPercent = 30 },
            new MembershipTier { Tier = 3, Name = "Kim cương", ThresholdPoints = 60_000, DiscountPercent = 50 });
        await db.SaveChangesAsync(ct);
    }

    private async Task SeedStaffAsync(CancellationToken ct)
    {
        if (await db.Staff.AnyAsync(ct)) return;
        var now = clock.GetUtcNow();
        Staff Make(string name, string email, string password, StaffRole role) => new()
        {
            FullName = name, Email = email, PasswordHash = Passwords.Hash(password), Role = role,
            CreatedAt = now, UpdatedAt = now,
        };
        // The BRD has no staff sign-up: the first Super Admin must come from seeding.
        db.Staff.AddRange(
            Make("Quản trị viên", "admin@techies.vn", "Admin@123", StaffRole.SuperAdmin),
            Make("Nhân viên Kho", "kho@techies.vn", "Staff@123", StaffRole.CatalogStaff),
            Make("Nhân viên Quầy", "quay@techies.vn", "Staff@123", StaffRole.OrderStaff));
        await db.SaveChangesAsync(ct);
    }
}

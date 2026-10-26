using Microsoft.EntityFrameworkCore;
using Techies.Web.Domain;

namespace Techies.Web.Data.Seed;

// The five coupons of ../techies order-service V8__coupons.sql, including the two that
// demonstrate rejection (expired, paused).
internal static class CouponSeed
{
    public static async Task SeedAsync(AppDbContext db, DateTimeOffset now, CancellationToken ct)
    {
        if (await db.Coupons.AnyAsync(ct)) return;
        var admin = await db.Staff.Where(s => s.RoleId == SeededRoles.SuperAdmin).Select(s => s.Id).FirstAsync(ct);
        var created = now.AddDays(-480);
        Coupon Make(string code, string description, decimal discount, decimal min, bool active = true, DateTimeOffset? expires = null) => new()
        {
            Code = code, Description = description, DiscountAmount = discount, MinSubtotal = min, Active = active,
            ExpiresAt = expires, CreatedBy = admin, CreatedAt = created,
        };
        db.Coupons.AddRange(
            Make("TECHIES50K", "Giảm 50.000đ cho đơn từ 500.000đ", 50_000, 500_000),
            Make("TECHIES500K", "Giảm 500.000đ cho đơn từ 10.000.000đ", 500_000, 10_000_000),
            Make("FREESHIP30K", "Giảm 30.000đ, áp dụng cho mọi đơn", 30_000, 0),
            Make("EXPIRED100K", "Đã hết hạn — minh họa trường hợp từ chối", 100_000, 0, expires: now.AddDays(-7)),
            Make("PAUSED200K", "Đang tạm dừng — minh họa trường hợp từ chối", 200_000, 0, active: false));
        await db.SaveChangesAsync(ct);
    }
}

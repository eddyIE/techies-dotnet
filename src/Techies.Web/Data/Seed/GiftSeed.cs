using Microsoft.EntityFrameworkCore;
using Techies.Web.Domain;

namespace Techies.Web.Data.Seed;

// Gifts from ../techies loyalty-service V1__init.sql: two at tier 0, one per higher tier,
// one out of stock and one nobody can afford.
internal static class GiftSeed
{
    public static async Task SeedAsync(AppDbContext db, DateTimeOffset now, CancellationToken ct)
    {
        if (await db.Gifts.AnyAsync(ct)) return;
        var created = now.AddDays(-480);
        Gift Make(string id, string name, string description, string imageSeed, long points, short minTier, int stock) => new()
        {
            Id = Guid.Parse(id), Name = name, Description = description,
            ImageUrl = $"https://picsum.photos/seed/{imageSeed}/400", PointsCost = points, MinTier = minTier,
            StockAvailable = stock, CreatedAt = created, UpdatedAt = created,
        };
        db.Gifts.AddRange(
            Make("ce2eb2ca-a432-5d29-9755-1f4f1abf2a14", "Ốp lưng silicon", "Ốp lưng silicon chống sốc, nhận tại cửa hàng Techies gần nhất.", "op-lung-silicon", 500, 0, 50),
            Make("fa31e0b6-78d5-5739-a025-965512251dd0", "Cáp sạc USB-C 1m", "Cáp sạc USB-C dài 1m, hỗ trợ sạc nhanh 20W.", "cap-usb-c", 800, 0, 40),
            Make("1d61e4c8-962b-55bd-b0c9-f6c1045571ab", "Tai nghe có dây", "Tai nghe nhét tai có dây kèm micro, jack 3.5mm.", "tai-nghe-day", 2000, 1, 25),
            Make("f0fb47a7-58e4-576e-9470-3b91dc212271", "Củ sạc nhanh 25W", "Củ sạc nhanh 25W chuẩn PD, tương thích iPhone và Android.", "sac-nhanh-25w", 5000, 2, 15),
            Make("5a895aa7-839d-5f07-9729-7f3a68806ac1", "Tai nghe Bluetooth", "Tai nghe không dây chống ồn chủ động, hộp sạc 24 giờ.", "tai-nghe-bluetooth", 12000, 3, 10),
            Make("e20b380a-f62c-56eb-9ba4-f7cbf404ee5c", "Pin sạc dự phòng 10.000mAh", "Pin sạc dự phòng 10.000mAh, hai cổng ra. Tạm thời hết hàng.", "pin-du-phong", 1500, 0, 0),
            Make("378bdc0f-8cea-57b3-a646-86b07ac39296", "Đồng hồ thông minh", "Đồng hồ thông minh đo nhịp tim và SpO2, pin 7 ngày.", "dong-ho-thong-minh", 999_999, 0, 5));
        await db.SaveChangesAsync(ct);
    }
}

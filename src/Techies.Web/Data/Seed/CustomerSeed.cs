using Microsoft.EntityFrameworkCore;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;

namespace Techies.Web.Data.Seed;

// 40 demo customers (khach01..khach40@example.com / Customer@123) with 1–2 addresses each.
// A fixed Random seed keeps names, phones and addresses identical on every machine.
internal static class CustomerSeed
{
    private static readonly string[] FamilyNames = ["Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng", "Bùi", "Đỗ"];
    private static readonly string[] MiddleNames = ["Văn", "Thị", "Minh", "Thanh", "Ngọc", "Đức", "Thu", "Hoài", "Quốc", "Gia"];
    private static readonly string[] GivenNames = ["An", "Bình", "Châu", "Dũng", "Giang", "Hà", "Hải", "Hùng", "Khoa", "Lan", "Linh", "Long", "Mai", "Nam", "Phúc", "Quân", "Tâm", "Thảo", "Trang", "Tuấn", "Vy", "Yến"];
    private static readonly (string Province, string District, string Ward)[] Places =
    [
        ("Hà Nội", "Cầu Giấy", "Dịch Vọng"), ("Hà Nội", "Đống Đa", "Láng Hạ"), ("Hà Nội", "Hoàn Kiếm", "Hàng Bạc"),
        ("TP. Hồ Chí Minh", "Quận 1", "Bến Nghé"), ("TP. Hồ Chí Minh", "Quận 3", "Võ Thị Sáu"), ("TP. Hồ Chí Minh", "Thủ Đức", "Linh Trung"),
        ("Đà Nẵng", "Hải Châu", "Thạch Thang"), ("Hải Phòng", "Lê Chân", "An Biên"), ("Cần Thơ", "Ninh Kiều", "An Hòa"),
    ];
    private static readonly string[] Streets = ["Trần Duy Hưng", "Nguyễn Trãi", "Lê Lợi", "Hai Bà Trưng", "Điện Biên Phủ", "Lý Thường Kiệt", "Nguyễn Huệ"];

    public const int Count = 40;
    public const string Password = "Customer@123";

    public static async Task SeedAsync(AppDbContext db, DateTimeOffset now, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(ct)) return;
        var random = new Random(2026);
        var hash = Passwords.Hash(Password); // same password for all, so hash once
        for (var i = 1; i <= Count; i++)
        {
            var name = $"{Pick(random, FamilyNames)} {Pick(random, MiddleNames)} {Pick(random, GivenNames)}";
            var phone = "09" + random.Next(10_000_000, 99_999_999);
            var created = now.AddDays(-random.Next(460, 500));
            var user = new User
            {
                Email = $"khach{i:00}@example.com", PasswordHash = hash, FullName = name, Phone = phone,
                CreatedAt = created, UpdatedAt = created,
                Loyalty = new CustomerLoyalty { Tier = 0, UpdatedAt = created },
            };
            var addressCount = random.Next(1, 3);
            for (var a = 0; a < addressCount; a++)
            {
                var place = Pick(random, Places);
                user.Addresses.Add(new Address
                {
                    RecipientName = name, Phone = phone,
                    Line1 = $"{random.Next(1, 300)} {Pick(random, Streets)}",
                    Ward = place.Ward, District = place.District, Province = place.Province,
                    IsDefault = a == 0, CreatedAt = created, UpdatedAt = created,
                });
            }
            db.Users.Add(user);
        }
        await db.SaveChangesAsync(ct);
    }

    private static T Pick<T>(Random random, T[] items) => items[random.Next(items.Length)];
}

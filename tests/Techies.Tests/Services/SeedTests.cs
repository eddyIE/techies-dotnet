using Microsoft.EntityFrameworkCore;
using Techies.Web.Data.Seed;
using Techies.Web.Domain;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class SeedTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Seeding_twice_creates_staff_and_tiers_once()
    {
        await using var db = await fixture.CreateContextAsync();
        var seeder = new DbSeeder(db, TimeProvider.System);

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        Assert.Equal(3, await db.Staff.CountAsync());
        Assert.Equal(4, await db.MembershipTiers.CountAsync());
        var admin = await db.Staff.SingleAsync(s => s.Email == "admin@techies.vn");
        Assert.Equal(StaffRole.SuperAdmin, admin.Role);
        Assert.True(BCrypt.Net.BCrypt.Verify("Admin@123", admin.PasswordHash));
    }
}

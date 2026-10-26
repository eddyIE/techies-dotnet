using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Techies.Web.Data.Seed;
using Techies.Web.Domain;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.Services;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class SmallGridTests(PostgresFixture fixture)
{
    private static GridRequest Request(string path, string query, string[] sorts, string sort) =>
        GridRequest.From(path, new QueryString(query), sorts, sort, false);

    [Theory]
    [InlineData("Running", new[] { "TECHIES50K", "TECHIES500K", "FREESHIP30K" })]
    [InlineData("Paused", new[] { "PAUSED200K" })]
    [InlineData("Expired", new[] { "EXPIRED100K" })]
    public async Task Coupon_status_filter_matches_computed_status(string status, string[] codes)
    {
        await using var db = await fixture.CreateContextAsync();
        await new DbSeeder(db, TimeProvider.System).SeedAsync();

        var (rows, _) = await new CouponService(db, TimeProvider.System)
            .GridAsync(Request("/admin/coupons", "?status=" + status, CouponService.GridSorts, "code"));

        Assert.Equal(codes.Order(), rows.Select(c => c.Code).Order());
    }

    [Fact]
    public async Task Gift_filters_tier_and_out_of_stock()
    {
        await using var db = await fixture.CreateContextAsync();
        await new DbSeeder(db, TimeProvider.System).SeedAsync();
        await db.Gifts.ExecuteUpdateAsync(u => u.SetProperty(g => g.StockAvailable, g => g.Name == "Pin sạc dự phòng 10.000mAh" ? 0 : 10));

        var (tier0Empty, _) = await new GiftService(db, TimeProvider.System)
            .GridAsync(Request("/admin/gifts", "?tier=0&stock_to=0", GiftService.GridSorts, "points"));

        Assert.Equal(["Pin sạc dự phòng 10.000mAh"], tier0Empty.Select(g => g.Name));
    }

    [Fact]
    public async Task Bulk_staff_lock_skips_self_and_locks_others()
    {
        await using var db = await fixture.CreateContextAsync();
        await new DbSeeder(db, TimeProvider.System).SeedAsync();
        var admin = await db.Staff.SingleAsync(s => s.Email == "admin@techies.vn");
        var others = await db.Staff.Where(s => s.Id != admin.Id).Select(s => s.Id).ToListAsync();
        var service = new StaffService(db, TimeProvider.System);

        var result = await Bulk.RunAsync([admin.Id, .. others], id => service.SetStatusAsync(id, AccountStatus.Locked, admin.Id));

        Assert.Equal((2, 3), (result.Done, result.Total));
        Assert.Contains("chính mình", result.Errors.Single());
        db.ChangeTracker.Clear();
        Assert.Equal(AccountStatus.Active, (await db.Staff.SingleAsync(s => s.Id == admin.Id)).Status);
    }

    [Fact]
    public async Task Bulk_role_delete_skips_assigned_roles()
    {
        await using var db = await fixture.CreateContextAsync();
        db.Staff.Add(new Staff { FullName = "A", Email = "a@x.vn", PasswordHash = "x", RoleId = SeededRoles.OrderStaff });
        await db.SaveChangesAsync();
        var service = new RoleService(db);

        var result = await Bulk.RunAsync([SeededRoles.CatalogStaff, SeededRoles.OrderStaff, SeededRoles.SuperAdmin], id => service.DeleteAsync(id));

        Assert.Equal((1, 3), (result.Done, result.Total));
        Assert.True(await db.Roles.AnyAsync(r => r.Id == SeededRoles.OrderStaff));
        Assert.False(await db.Roles.AnyAsync(r => r.Id == SeededRoles.CatalogStaff));
    }
}

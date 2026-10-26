using Microsoft.EntityFrameworkCore;
using Techies.Web.Data.Seed;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Services;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class PermissionTests(PostgresFixture fixture)
{
    private static async Task<StaffAccess> AccessOfAsync(Web.Data.AppDbContext db, string email)
    {
        var id = await db.Staff.Where(s => s.Email == email).Select(s => s.Id).SingleAsync();
        return await new PermissionService(db).ForStaffAsync(id);
    }

    [Fact]
    public async Task Seeded_staff_keep_the_access_of_the_old_fixed_roles()
    {
        await using var db = await fixture.CreateContextAsync();
        await new DbSeeder(db, TimeProvider.System).SeedAsync();

        var admin = await AccessOfAsync(db, "admin@techies.vn");
        var catalog = await AccessOfAsync(db, "kho@techies.vn");
        var counter = await AccessOfAsync(db, "quay@techies.vn");

        Assert.True(admin.IsSuperAdmin);
        Assert.All(Permissions.All, p => Assert.True(admin.Has(p.Code)));
        Assert.Equal(
            ["categories.manage", "inventory.count", "inventory.import", "inventory.view", "products.edit", "products.toggle", "products.view"],
            catalog.Codes.Order());
        Assert.Equal(
            ["customers.lock", "customers.view", "giftcounter.claim", "giftcounter.lookup", "orders.cancel", "orders.complete",
             "orders.print", "orders.view", "reviews.view"],
            counter.Codes.Order());
        Assert.False(catalog.IsSuperAdmin);
        Assert.False(counter.Has(Permissions.DashboardView));
    }

    [Fact]
    public async Task Custom_role_grants_exactly_its_permissions_and_edits_apply_on_next_lookup()
    {
        await using var db = await fixture.CreateContextAsync();
        var role = new Role { Code = "KE_TOAN", Name = "Kế toán" };
        role.Permissions.Add(new RolePermission { PermissionId = Permissions.Find(Permissions.DashboardView).Id });
        db.Roles.Add(role);
        var staff = new Staff { FullName = "Kế toán", Email = "kt@x.vn", PasswordHash = "x", Role = role };
        db.Staff.Add(staff);
        await db.SaveChangesAsync();

        var before = await new PermissionService(db).ForStaffAsync(staff.Id);
        db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = Permissions.Find(Permissions.OrdersView).Id });
        await db.SaveChangesAsync();
        var after = await new PermissionService(db).ForStaffAsync(staff.Id);

        Assert.Equal(["dashboard.view"], before.Codes);
        Assert.True(after.Has(Permissions.OrdersView));
        Assert.False(after.Has(Permissions.OrdersComplete));
    }
}

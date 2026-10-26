using Microsoft.EntityFrameworkCore;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Services;
using Techies.Web.ViewModels.Staff;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class RoleServiceTests(PostgresFixture fixture)
{
    private static RoleForm Form(string name, params string[] permissions) => new() { Name = name, Permissions = [.. permissions] };

    [Fact]
    public async Task Create_saves_role_with_its_permissions()
    {
        await using var db = await fixture.CreateContextAsync();

        var id = await new RoleService(db).CreateAsync(Form("Kế toán", Permissions.DashboardView, Permissions.OrdersView));

        var role = await db.Roles.Include(r => r.Permissions).SingleAsync(r => r.Id == id);
        Assert.Equal("KE_TOAN", role.Code);
        Assert.Equal(2, role.Permissions.Count);
    }

    [Fact]
    public async Task Role_names_are_unique_and_unknown_permissions_rejected()
    {
        await using var db = await fixture.CreateContextAsync();
        var service = new RoleService(db);

        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(Form("Nhân viên Kho & Sản phẩm")));
        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(Form("Mới", "orders.fly")));
    }

    [Fact]
    public async Task Update_replaces_the_permission_set()
    {
        await using var db = await fixture.CreateContextAsync();
        var service = new RoleService(db);

        await service.UpdateAsync(SeededRoles.CatalogStaff, Form("Nhân viên Kho", Permissions.InventoryView));

        db.ChangeTracker.Clear();
        var role = await db.Roles.Include(r => r.Permissions).SingleAsync(r => r.Id == SeededRoles.CatalogStaff);
        Assert.Equal("Nhân viên Kho", role.Name);
        Assert.Equal([Permissions.Find(Permissions.InventoryView).Id], role.Permissions.Select(p => p.PermissionId));
    }

    [Fact]
    public async Task Delete_is_blocked_while_staff_have_the_role()
    {
        await using var db = await fixture.CreateContextAsync();
        db.Staff.Add(new Staff { FullName = "A", Email = "a@x.vn", PasswordHash = "x", RoleId = SeededRoles.OrderStaff });
        await db.SaveChangesAsync();
        var service = new RoleService(db);

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.DeleteAsync(SeededRoles.OrderStaff));
        Assert.Contains("1 nhân viên", ex.Message);

        await service.DeleteAsync(SeededRoles.CatalogStaff); // unassigned: allowed
        Assert.False(await db.Roles.AnyAsync(r => r.Id == SeededRoles.CatalogStaff));
    }

    [Fact]
    public async Task System_role_cannot_be_deleted()
    {
        await using var db = await fixture.CreateContextAsync();

        await Assert.ThrowsAsync<DomainException>(() => new RoleService(db).DeleteAsync(SeededRoles.SuperAdmin));
    }

    [Fact]
    public async Task System_role_can_be_renamed_but_keeps_implicit_full_access()
    {
        await using var db = await fixture.CreateContextAsync();
        var service = new RoleService(db);

        await service.UpdateAsync(SeededRoles.SuperAdmin, Form("Quản trị hệ thống", Permissions.OrdersView));

        db.ChangeTracker.Clear();
        var role = await db.Roles.Include(r => r.Permissions).SingleAsync(r => r.Id == SeededRoles.SuperAdmin);
        Assert.Equal("Quản trị hệ thống", role.Name);
        Assert.Empty(role.Permissions); // still implicit "everything"; submitted permissions are ignored
        Assert.True(role.IsSystem);
    }
}

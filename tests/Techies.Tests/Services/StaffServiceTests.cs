using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Services;
using Techies.Web.ViewModels.Staff;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class StaffServiceTests(PostgresFixture fixture)
{
    private static StaffService Service(AppDbContext db) => new(db, TimeProvider.System);

    private static async Task<Staff> AddAsync(AppDbContext db, int role, string email, AccountStatus status = AccountStatus.Active)
    {
        var staff = new Staff { FullName = email, Email = email, PasswordHash = "x", RoleId = role, Status = status };
        db.Staff.Add(staff);
        await db.SaveChangesAsync();
        return staff;
    }

    [Fact]
    public async Task Create_makes_active_staff_with_hashed_password_and_rejects_duplicate_email()
    {
        await using var db = await fixture.CreateContextAsync();
        var form = new StaffForm { FullName = "Nhân viên Mới", Email = "Moi@Techies.vn", Phone = "0900000000", Password = "Batdau@123", RoleId = SeededRoles.OrderStaff };

        var created = await Service(db).CreateAsync(form);

        Assert.Equal(("moi@techies.vn", AccountStatus.Active), (created.Email, created.Status));
        Assert.True(Passwords.Verify("Batdau@123", created.PasswordHash));
        await Assert.ThrowsAsync<DomainException>(() => Service(db).CreateAsync(form));
    }

    [Fact]
    public async Task Super_admin_cannot_lock_themselves()
    {
        await using var db = await fixture.CreateContextAsync();
        var admin = await AddAsync(db, SeededRoles.SuperAdmin, "a@x.vn");
        await AddAsync(db, SeededRoles.SuperAdmin, "b@x.vn");

        await Assert.ThrowsAsync<DomainException>(() => Service(db).ToggleLockAsync(admin.Id, actingStaffId: admin.Id));
    }

    [Fact]
    public async Task Super_admin_cannot_demote_themselves()
    {
        await using var db = await fixture.CreateContextAsync();
        var admin = await AddAsync(db, SeededRoles.SuperAdmin, "a@x.vn");
        await AddAsync(db, SeededRoles.SuperAdmin, "b@x.vn");

        await Assert.ThrowsAsync<DomainException>(() => Service(db).UpdateAsync(admin.Id,
            new StaffForm { FullName = "A", Email = "a@x.vn", RoleId = SeededRoles.OrderStaff }, actingStaffId: admin.Id));
    }

    [Fact]
    public async Task Last_active_super_admin_cannot_be_locked_or_demoted()
    {
        await using var db = await fixture.CreateContextAsync();
        var admin = await AddAsync(db, SeededRoles.SuperAdmin, "a@x.vn");
        await AddAsync(db, SeededRoles.SuperAdmin, "locked@x.vn", AccountStatus.Locked);
        var other = await AddAsync(db, SeededRoles.CatalogStaff, "kho@x.vn");

        await Assert.ThrowsAsync<DomainException>(() => Service(db).ToggleLockAsync(admin.Id, actingStaffId: other.Id));
        await Assert.ThrowsAsync<DomainException>(() => Service(db).UpdateAsync(admin.Id,
            new StaffForm { FullName = "A", Email = "a@x.vn", RoleId = SeededRoles.CatalogStaff }, actingStaffId: other.Id));
    }

    [Fact]
    public async Task Super_admin_can_lock_another_staff_and_change_their_role()
    {
        await using var db = await fixture.CreateContextAsync();
        var admin = await AddAsync(db, SeededRoles.SuperAdmin, "a@x.vn");
        var clerk = await AddAsync(db, SeededRoles.OrderStaff, "quay@x.vn");
        var service = Service(db);

        await service.UpdateAsync(clerk.Id, new StaffForm { FullName = "Quầy", Email = "quay@x.vn", RoleId = SeededRoles.CatalogStaff }, admin.Id);
        var status = await service.ToggleLockAsync(clerk.Id, admin.Id);

        db.ChangeTracker.Clear();
        var saved = await db.Staff.SingleAsync(s => s.Id == clerk.Id);
        Assert.Equal((SeededRoles.CatalogStaff, AccountStatus.Locked), (saved.RoleId, saved.Status));
        Assert.Equal(AccountStatus.Locked, status);
    }

    [Fact]
    public async Task Reset_password_requires_8_characters()
    {
        await using var db = await fixture.CreateContextAsync();
        var clerk = await AddAsync(db, SeededRoles.OrderStaff, "quay@x.vn");
        var service = Service(db);

        await Assert.ThrowsAsync<DomainException>(() => service.ResetPasswordAsync(clerk.Id, "ngan"));
        await service.ResetPasswordAsync(clerk.Id, "Matkhau@moi");

        db.ChangeTracker.Clear();
        Assert.True(Passwords.Verify("Matkhau@moi", (await db.Staff.SingleAsync()).PasswordHash));
    }

    [Fact]
    public async Task Two_super_admins_locking_each_other_at_once_leave_one_active()
    {
        await using var db = await fixture.CreateContextAsync();
        var a = await AddAsync(db, SeededRoles.SuperAdmin, "a@x.vn");
        var b = await AddAsync(db, SeededRoles.SuperAdmin, "b@x.vn");
        await using var dbA = PostgresFixture.CreateSibling(db);
        await using var dbB = PostgresFixture.CreateSibling(db);

        // A holds the Super Admin lock while B tries to lock A; B must wait and then see A's change.
        await using var hold = await dbA.Database.BeginTransactionAsync();
        await dbA.Database.ExecuteSqlAsync($"SELECT id FROM staff WHERE role_id = 1 FOR UPDATE");
        var bLocksA = Attempt(Service(dbB).ToggleLockAsync(a.Id, actingStaffId: b.Id));
        await Task.Delay(300);
        await dbA.Database.ExecuteSqlAsync($"UPDATE staff SET status = 'LOCKED' WHERE id = {b.Id}");
        await hold.CommitAsync();

        Assert.False(await bLocksA);
        db.ChangeTracker.Clear();
        Assert.Equal(1, await db.Staff.CountAsync(s => s.RoleId == SeededRoles.SuperAdmin && s.Status == AccountStatus.Active));
    }

    private static async Task<bool> Attempt(Task task)
    {
        try { await task; return true; }
        catch (DomainException) { return false; }
    }
}

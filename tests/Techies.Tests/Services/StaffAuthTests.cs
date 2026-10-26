using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Services;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class StaffAuthTests(PostgresFixture fixture)
{
    private static async Task<Staff> AddStaffAsync(AppDbContext db, AccountStatus status = AccountStatus.Active)
    {
        var staff = new Staff
        {
            FullName = "Nhân viên A", Email = "a@techies.vn", PasswordHash = Passwords.Hash("Secret@123"),
            RoleId = SeededRoles.OrderStaff, Status = status,
        };
        db.Staff.Add(staff);
        await db.SaveChangesAsync();
        return staff;
    }

    [Fact]
    public async Task Authenticate_accepts_correct_password_with_any_email_case()
    {
        await using var db = await fixture.CreateContextAsync();
        var staff = await AddStaffAsync(db);

        var result = await new StaffAuthService(db).AuthenticateAsync("  A@Techies.VN ", "Secret@123");

        Assert.Equal(staff.Id, result.Id);
    }

    [Fact]
    public async Task Authenticate_rejects_wrong_password()
    {
        await using var db = await fixture.CreateContextAsync();
        await AddStaffAsync(db);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => new StaffAuthService(db).AuthenticateAsync("a@techies.vn", "wrong"));
        Assert.Equal("Email hoặc mật khẩu không đúng.", ex.Message);
    }

    [Fact]
    public async Task Authenticate_rejects_locked_account()
    {
        await using var db = await fixture.CreateContextAsync();
        await AddStaffAsync(db, AccountStatus.Locked);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => new StaffAuthService(db).AuthenticateAsync("a@techies.vn", "Secret@123"));
        Assert.Equal("Tài khoản đã bị khóa.", ex.Message);
    }

    [Fact]
    public async Task Session_stays_valid_while_active_with_same_role()
    {
        await using var db = await fixture.CreateContextAsync();
        var staff = await AddStaffAsync(db);

        Assert.True(await new AccountStatusService(db).IsStaffSessionValidAsync(staff.Id, SeededRoles.OrderStaff));
    }

    [Fact]
    public async Task Session_is_rejected_after_lock()
    {
        await using var db = await fixture.CreateContextAsync();
        var staff = await AddStaffAsync(db);
        staff.Status = AccountStatus.Locked;
        await db.SaveChangesAsync();

        Assert.False(await new AccountStatusService(db).IsStaffSessionValidAsync(staff.Id, SeededRoles.OrderStaff));
    }

    [Fact]
    public async Task Session_is_rejected_after_role_change()
    {
        await using var db = await fixture.CreateContextAsync();
        var staff = await AddStaffAsync(db);
        staff.RoleId = SeededRoles.CatalogStaff;
        await db.SaveChangesAsync();

        Assert.False(await new AccountStatusService(db).IsStaffSessionValidAsync(staff.Id, SeededRoles.OrderStaff));
    }
}

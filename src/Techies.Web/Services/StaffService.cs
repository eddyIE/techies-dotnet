using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.ViewModels.Staff;

namespace Techies.Web.Services;

// BRD §7: Super Admin manages staff accounts. No hard delete: leavers are locked so the
// orders, stock changes and gift hand-overs they handled keep pointing at them.
public sealed class StaffService(AppDbContext db, TimeProvider clock)
{
    public Task<List<Staff>> ListAsync(CancellationToken ct = default) =>
        db.Staff.AsNoTracking().OrderBy(s => s.Role).ThenBy(s => s.FullName).ToListAsync(ct);

    public async Task<Staff> GetAsync(Guid id, CancellationToken ct = default) =>
        await db.Staff.FindAsync([id], ct) ?? throw new DomainException("Không tìm thấy nhân viên.");

    public async Task<Staff> CreateAsync(StaffForm form, CancellationToken ct = default)
    {
        var email = Emails.Normalize(form.Email);
        if (await db.Staff.AnyAsync(s => s.Email == email, ct))
            throw new DomainException("Email này đã được dùng cho một nhân viên khác.");
        RequireStrongPassword(form.Password);
        var now = clock.GetUtcNow();
        var staff = new Staff
        {
            FullName = form.FullName.Trim(), Email = email, Phone = Clean(form.Phone), Role = form.Role,
            PasswordHash = Passwords.Hash(form.Password!), Status = AccountStatus.Active, CreatedAt = now, UpdatedAt = now,
        };
        db.Staff.Add(staff);
        await db.SaveChangesAsync(ct);
        return staff;
    }

    // Name, phone and role (a role change ends that person's current session).
    public async Task UpdateAsync(Guid id, StaffForm form, Guid actingStaffId, CancellationToken ct = default)
    {
        await using var tx = await BeginGuardedAsync(ct);
        var staff = await LockedAsync(id, ct);
        if (staff.Role == StaffRole.SuperAdmin && form.Role != StaffRole.SuperAdmin)
        {
            if (id == actingStaffId) throw new DomainException("Bạn không thể tự hạ quyền của chính mình.");
            await EnsureAnotherActiveSuperAdminAsync(id, ct);
        }
        staff.FullName = form.FullName.Trim();
        staff.Phone = Clean(form.Phone);
        staff.Role = form.Role;
        staff.UpdatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task ResetPasswordAsync(Guid id, string newPassword, CancellationToken ct = default)
    {
        RequireStrongPassword(newPassword);
        var staff = await GetAsync(id, ct);
        staff.PasswordHash = Passwords.Hash(newPassword);
        staff.UpdatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    // Locking takes effect on the person's next request (cookie re-validation).
    public async Task<AccountStatus> ToggleLockAsync(Guid id, Guid actingStaffId, CancellationToken ct = default)
    {
        await using var tx = await BeginGuardedAsync(ct);
        var staff = await LockedAsync(id, ct);
        if (staff.Status == AccountStatus.Active)
        {
            if (id == actingStaffId) throw new DomainException("Bạn không thể tự khóa tài khoản của chính mình.");
            if (staff.Role == StaffRole.SuperAdmin) await EnsureAnotherActiveSuperAdminAsync(id, ct);
        }
        staff.Status = staff.Status == AccountStatus.Active ? AccountStatus.Locked : AccountStatus.Active;
        staff.UpdatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return staff.Status;
    }

    // Lock every Super Admin row first, so two admins locking or demoting each other at the
    // same time are serialized and the second one sees the first one's change.
    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginGuardedAsync(CancellationToken ct)
    {
        var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlAsync($"SELECT id FROM staff WHERE role = 'SUPER_ADMIN' FOR UPDATE", ct);
        return tx;
    }

    private async Task<Staff> LockedAsync(Guid id, CancellationToken ct)
    {
        var staff = await db.Staff.FromSql($"SELECT * FROM staff WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(ct)
            ?? throw new DomainException("Không tìm thấy nhân viên.");
        await db.Entry(staff).ReloadAsync(ct);
        return staff;
    }

    // Someone must always be able to manage staff.
    private async Task EnsureAnotherActiveSuperAdminAsync(Guid exceptId, CancellationToken ct)
    {
        if (!await db.Staff.AnyAsync(s => s.Id != exceptId && s.Role == StaffRole.SuperAdmin && s.Status == AccountStatus.Active, ct))
            throw new DomainException("Phải luôn còn ít nhất một Quản trị viên cao cấp đang hoạt động.");
    }

    private static void RequireStrongPassword(string? password)
    {
        if (password is null || password.Length < 8) throw new DomainException("Mật khẩu tối thiểu 8 ký tự.");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

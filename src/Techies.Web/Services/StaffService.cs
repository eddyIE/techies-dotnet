using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.ViewModels.Staff;

namespace Techies.Web.Services;

// BRD §7: Super Admin manages staff accounts. No hard delete: leavers are locked so the
// orders, stock changes and gift hand-overs they handled keep pointing at them.
public sealed class StaffService(AppDbContext db, TimeProvider clock)
{
    public static readonly string[] GridSorts = ["name", "email", "role", "created"];

    public async Task<(List<Staff> Rows, int Total)> GridAsync(GridRequest r, CancellationToken ct = default)
    {
        var query = db.Staff.AsNoTracking().Include(s => s.Role).AsQueryable();
        if (r.Text("q") is { } q)
        {
            var pattern = QueryExtensions.Contains(q);
            query = query.Where(s => EF.Functions.ILike(s.FullName, pattern) || EF.Functions.ILike(s.Email, pattern)
                || (s.Phone != null && EF.Functions.ILike(s.Phone, pattern)));
        }
        if (r.Int("role") is { } roleId) query = query.Where(s => s.RoleId == roleId);
        if (r.Enum<AccountStatus>("status") is { } status) query = query.Where(s => s.Status == status);

        query = (r.Sort, r.Desc) switch
        {
            ("email", false) => query.OrderBy(s => s.Email),
            ("email", true) => query.OrderByDescending(s => s.Email),
            ("role", false) => query.OrderBy(s => s.Role.Name).ThenBy(s => s.FullName),
            ("role", true) => query.OrderByDescending(s => s.Role.Name).ThenBy(s => s.FullName),
            ("created", false) => query.OrderBy(s => s.CreatedAt),
            ("created", true) => query.OrderByDescending(s => s.CreatedAt),
            (_, true) => query.OrderByDescending(s => s.FullName),
            _ => query.OrderBy(s => s.FullName),
        };
        return (await query.Skip((r.Page - 1) * r.PageSize).Take(r.PageSize).ToListAsync(ct), await query.CountAsync(ct));
    }

    public async Task<Staff> GetAsync(Guid id, CancellationToken ct = default) =>
        await db.Staff.FindAsync([id], ct) ?? throw new DomainException("Không tìm thấy nhân viên.");

    public async Task<Staff> CreateAsync(StaffForm form, CancellationToken ct = default)
    {
        var email = Emails.Normalize(form.Email);
        if (await db.Staff.AnyAsync(s => s.Email == email, ct))
            throw new DomainException("Email này đã được dùng cho một nhân viên khác.");
        RequireStrongPassword(form.Password);
        await RequireRoleAsync(form.RoleId, ct);
        var now = clock.GetUtcNow();
        var staff = new Staff
        {
            FullName = form.FullName.Trim(), Email = email, Phone = Clean(form.Phone), RoleId = form.RoleId,
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
        await RequireRoleAsync(form.RoleId, ct);
        if (staff.RoleId == SeededRoles.SuperAdmin && form.RoleId != SeededRoles.SuperAdmin)
        {
            if (id == actingStaffId) throw new DomainException("Bạn không thể tự hạ quyền của chính mình.");
            await EnsureAnotherActiveSuperAdminAsync(id, ct);
        }
        staff.FullName = form.FullName.Trim();
        staff.Phone = Clean(form.Phone);
        staff.RoleId = form.RoleId;
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
        var current = await db.Staff.AsNoTracking().Where(s => s.Id == id).Select(s => (AccountStatus?)s.Status).SingleOrDefaultAsync(ct)
            ?? throw new DomainException("Không tìm thấy nhân viên.");
        var target = current == AccountStatus.Active ? AccountStatus.Locked : AccountStatus.Active;
        await SetStatusAsync(id, target, actingStaffId, ct);
        return target;
    }

    // Explicit target status, for mass actions over mixed rows.
    public async Task SetStatusAsync(Guid id, AccountStatus target, Guid actingStaffId, CancellationToken ct = default)
    {
        await using var tx = await BeginGuardedAsync(ct);
        var staff = await LockedAsync(id, ct);
        if (staff.Status == AccountStatus.Active && target == AccountStatus.Locked)
        {
            if (id == actingStaffId) throw new DomainException("Bạn không thể tự khóa tài khoản của chính mình.");
            if (staff.RoleId == SeededRoles.SuperAdmin) await EnsureAnotherActiveSuperAdminAsync(id, ct);
        }
        staff.Status = target;
        staff.UpdatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    // Lock every Super Admin row first, so two admins locking or demoting each other at the
    // same time are serialized and the second one sees the first one's change.
    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginGuardedAsync(CancellationToken ct)
    {
        var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlAsync($"SELECT id FROM staff WHERE role_id = {SeededRoles.SuperAdmin} FOR UPDATE", ct);
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
        if (!await db.Staff.AnyAsync(s => s.Id != exceptId && s.RoleId == SeededRoles.SuperAdmin && s.Status == AccountStatus.Active, ct))
            throw new DomainException("Phải luôn còn ít nhất một Quản trị viên cao cấp đang hoạt động.");
    }

    private async Task RequireRoleAsync(int roleId, CancellationToken ct)
    {
        if (!await db.Roles.AnyAsync(r => r.Id == roleId, ct)) throw new DomainException("Vai trò không tồn tại.");
    }

    private static void RequireStrongPassword(string? password)
    {
        if (password is null || password.Length < 8) throw new DomainException("Mật khẩu tối thiểu 8 ký tự.");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

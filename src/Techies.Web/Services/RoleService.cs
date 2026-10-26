using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.ViewModels.Staff;

namespace Techies.Web.Services;

// BRD §7 "Thiết lập Vai trò & Phân quyền", managed by the Super Admin.
public sealed class RoleService(AppDbContext db)
{
    public Task<List<Role>> ListAsync(CancellationToken ct = default) =>
        db.Roles.AsNoTracking().OrderBy(r => r.Id).ToListAsync(ct);

    public static readonly string[] GridSorts = ["name", "staff", "permissions"];

    public async Task<(List<RoleRow> Rows, int Total)> GridAsync(GridRequest r, CancellationToken ct = default)
    {
        var query = db.Roles.AsNoTracking();
        if (r.Text("q") is { } q) query = query.Where(x => EF.Functions.ILike(x.Name, QueryExtensions.Contains(q)));
        query = (r.Sort, r.Desc) switch
        {
            ("staff", false) => query.OrderBy(x => db.Staff.Count(s => s.RoleId == x.Id)).ThenBy(x => x.Name),
            ("staff", true) => query.OrderByDescending(x => db.Staff.Count(s => s.RoleId == x.Id)).ThenBy(x => x.Name),
            ("permissions", false) => query.OrderBy(x => x.IsSystem).ThenBy(x => x.Permissions.Count),
            ("permissions", true) => query.OrderByDescending(x => x.IsSystem).ThenByDescending(x => x.Permissions.Count),
            ("name", true) => query.OrderByDescending(x => x.Name),
            ("name", false) => query.OrderBy(x => x.Name),
            _ => query.OrderBy(x => x.Id),
        };
        var rows = await query.Skip((r.Page - 1) * r.PageSize).Take(r.PageSize)
            .Select(x => new RoleRow(x.Id, x.Name, x.Description, x.IsSystem, db.Staff.Count(s => s.RoleId == x.Id),
                x.IsSystem ? Permissions.All.Count : x.Permissions.Count))
            .ToListAsync(ct);
        return (rows, await query.CountAsync(ct));
    }

    public async Task<Role> GetAsync(int id, CancellationToken ct = default) =>
        await db.Roles.Include(r => r.Permissions).SingleOrDefaultAsync(r => r.Id == id, ct)
        ?? throw new DomainException("Không tìm thấy vai trò.");

    public async Task<int> CreateAsync(RoleForm form, CancellationToken ct = default)
    {
        var role = new Role { Code = await UniqueCodeAsync(form.Name, ct) };
        await ApplyAsync(role, form, ct);
        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);
        return role.Id;
    }

    public async Task UpdateAsync(int id, RoleForm form, CancellationToken ct = default)
    {
        var role = await GetAsync(id, ct);
        if (role.IsSystem) throw new DomainException("Vai trò Quản trị viên cao cấp là vai trò hệ thống, không thể chỉnh sửa.");
        db.RolePermissions.RemoveRange(role.Permissions);
        await ApplyAsync(role, form, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var role = await GetAsync(id, ct);
        if (role.IsSystem) throw new DomainException("Không thể xóa vai trò hệ thống.");
        var assigned = await db.Staff.CountAsync(s => s.RoleId == id, ct);
        if (assigned > 0)
            throw new DomainException($"Vai trò “{role.Name}” đang được gán cho {assigned} nhân viên. Hãy đổi vai trò của họ trước.");
        db.Roles.Remove(role);
        await db.SaveChangesAsync(ct);
    }

    private async Task ApplyAsync(Role role, RoleForm form, CancellationToken ct)
    {
        var name = form.Name.Trim();
        if (name.Length == 0) throw new DomainException("Vui lòng nhập tên vai trò.");
        if (await db.Roles.AnyAsync(r => r.Name == name && r.Id != role.Id, ct))
            throw new DomainException("Tên vai trò đã tồn tại.");
        var unknown = form.Permissions.Where(c => !Permissions.All.Any(p => p.Code == c)).ToList();
        if (unknown.Count > 0) throw new DomainException("Quyền không hợp lệ: " + string.Join(", ", unknown));

        role.Name = name;
        role.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
        role.Permissions = form.Permissions.Distinct()
            .Select(c => new RolePermission { RoleId = role.Id, PermissionId = Permissions.Find(c).Id }).ToList();
    }

    // "Kế toán" -> "KE_TOAN" (a stable technical code; the name stays editable).
    private async Task<string> UniqueCodeAsync(string name, CancellationToken ct)
    {
        var baseCode = Slug.From(name).Replace('-', '_').ToUpperInvariant();
        if (baseCode.Length == 0) baseCode = "ROLE";
        baseCode = baseCode[..Math.Min(baseCode.Length, 28)];
        var taken = await db.Roles.Where(r => r.Code.StartsWith(baseCode)).Select(r => r.Code).ToListAsync(ct);
        if (!taken.Contains(baseCode)) return baseCode;
        var n = 2;
        while (taken.Contains($"{baseCode}_{n}")) n++;
        return $"{baseCode}_{n}";
    }
}

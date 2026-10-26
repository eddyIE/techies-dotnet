using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Infrastructure;

namespace Techies.Web.Services;

public sealed record StaffAccess(bool IsSuperAdmin, IReadOnlySet<string> Codes)
{
    public static readonly StaffAccess None = new(false, new HashSet<string>());

    // The system role has every permission, including ones added to the catalogue later.
    public bool Has(string permission) => IsSuperAdmin || Codes.Contains(permission);
}

public sealed class PermissionService(AppDbContext db)
{
    public async Task<StaffAccess> ForStaffAsync(Guid staffId, CancellationToken ct = default)
    {
        var role = await db.Staff.AsNoTracking().Where(s => s.Id == staffId)
            .Select(s => new { s.Role.IsSystem, Codes = s.Role.Permissions.Select(rp => db.Permissions.Single(p => p.Id == rp.PermissionId).Code).ToList() })
            .SingleOrDefaultAsync(ct);
        if (role is null) return StaffAccess.None;
        return role.IsSystem
            ? new StaffAccess(true, Permissions.All.Select(p => p.Code).ToHashSet())
            : new StaffAccess(false, role.Codes.ToHashSet());
    }
}

// The signed-in staff member's access, looked up once per request (so a role edit applies on
// the next request) and shared by authorization handlers, the menu and views.
public sealed class CurrentStaff(IHttpContextAccessor http, PermissionService permissions)
{
    private Task<StaffAccess>? _access;

    public Task<StaffAccess> AccessAsync()
    {
        var user = http.HttpContext?.User;
        return _access ??= user?.Identity?.AuthenticationType == AuthSchemes.Staff
            ? permissions.ForStaffAsync(user.UserId())
            : Task.FromResult(StaffAccess.None);
    }

    public async Task<bool> HasAsync(string permission) => (await AccessAsync()).Has(permission);
}

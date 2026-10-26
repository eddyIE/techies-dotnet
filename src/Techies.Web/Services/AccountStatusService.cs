using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;

namespace Techies.Web.Services;

// Checked on every authenticated request, so locking an account (or changing a staff role)
// ends existing sessions immediately — BRD §6/§7.
public sealed class AccountStatusService(AppDbContext db)
{
    public Task<bool> IsStaffSessionValidAsync(Guid staffId, StaffRole role, CancellationToken ct = default) =>
        db.Staff.AnyAsync(s => s.Id == staffId && s.Status == AccountStatus.Active && s.Role == role, ct);

    public Task<bool> IsCustomerSessionValidAsync(Guid userId, CancellationToken ct = default) =>
        db.Users.AnyAsync(u => u.Id == userId && u.Status == AccountStatus.Active, ct);
}

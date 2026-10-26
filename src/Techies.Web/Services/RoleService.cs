using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;

namespace Techies.Web.Services;

public sealed class RoleService(AppDbContext db)
{
    public Task<List<Role>> ListAsync(CancellationToken ct = default) =>
        db.Roles.AsNoTracking().OrderBy(r => r.Id).ToListAsync(ct);
}

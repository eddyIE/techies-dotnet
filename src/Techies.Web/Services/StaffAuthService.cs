using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;

namespace Techies.Web.Services;

public sealed class StaffAuthService(AppDbContext db)
{
    public async Task<Staff> AuthenticateAsync(string email, string password, CancellationToken ct = default)
    {
        var normalized = Emails.Normalize(email);
        var staff = await db.Staff.AsNoTracking().SingleOrDefaultAsync(s => s.Email == normalized, ct);
        if (staff is null || !Passwords.Verify(password, staff.PasswordHash))
            throw new DomainException("Email hoặc mật khẩu không đúng.");
        if (staff.Status == AccountStatus.Locked)
            throw new DomainException("Tài khoản đã bị khóa.");
        return staff;
    }
}

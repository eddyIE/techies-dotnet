using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.ViewModels.Account;

namespace Techies.Web.Services;

public sealed class CustomerAccountService(AppDbContext db, TimeProvider clock)
{
    public async Task<User> RegisterAsync(RegisterForm form, CancellationToken ct = default)
    {
        var email = Emails.Normalize(form.Email);
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new DomainException("Email đã được sử dụng.");

        var now = clock.GetUtcNow();
        var user = new User
        {
            Email = email, PasswordHash = Passwords.Hash(form.Password), FullName = form.FullName.Trim(),
            Phone = form.Phone.Trim(), CreatedAt = now, UpdatedAt = now,
            Loyalty = new CustomerLoyalty { Tier = 0, UpdatedAt = now },
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<User> AuthenticateAsync(string email, string password, CancellationToken ct = default)
    {
        var normalized = Emails.Normalize(email);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Email == normalized, ct);
        if (user is null || !Passwords.Verify(password, user.PasswordHash))
            throw new DomainException("Email hoặc mật khẩu không đúng.");
        if (user.Status == AccountStatus.Locked)
            throw new DomainException("Tài khoản đã bị khóa.");
        return user;
    }
}

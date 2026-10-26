using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.ViewModels.Account;
using Techies.Web.ViewModels.Shop;

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

    public Task<List<Address>> AddressesAsync(Guid userId, CancellationToken ct = default) =>
        db.Addresses.AsNoTracking().Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault).ThenByDescending(a => a.CreatedAt).ToListAsync(ct);

    // Resolves the checkout form to a shipping snapshot, saving a newly entered address.
    public async Task<ShippingAddress> ResolveShippingAsync(Guid userId, CheckoutForm form, CancellationToken ct = default)
    {
        if (form.AddressId is { } addressId)
        {
            var saved = await db.Addresses.AsNoTracking().SingleOrDefaultAsync(a => a.Id == addressId && a.UserId == userId, ct)
                ?? throw new DomainException("Không tìm thấy địa chỉ đã chọn.");
            return new ShippingAddress(saved.RecipientName, saved.Phone, saved.Line1, saved.Ward, saved.District, saved.Province);
        }

        var now = clock.GetUtcNow();
        var hasAny = await db.Addresses.AnyAsync(a => a.UserId == userId, ct);
        var makeDefault = form.MakeDefault || !hasAny;
        if (makeDefault)
            await db.Addresses.Where(a => a.UserId == userId && a.IsDefault)
                .ExecuteUpdateAsync(u => u.SetProperty(a => a.IsDefault, false), ct);
        var address = new Address
        {
            UserId = userId, RecipientName = form.RecipientName!.Trim(), Phone = form.Phone!.Trim(), Line1 = form.Line1!.Trim(),
            Ward = form.Ward!.Trim(), District = form.District!.Trim(), Province = form.Province!.Trim(),
            IsDefault = makeDefault, CreatedAt = now, UpdatedAt = now,
        };
        db.Addresses.Add(address);
        await db.SaveChangesAsync(ct);
        return new ShippingAddress(address.RecipientName, address.Phone, address.Line1, address.Ward, address.District, address.Province);
    }
}

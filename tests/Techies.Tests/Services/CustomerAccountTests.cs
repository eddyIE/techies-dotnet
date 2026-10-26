using Microsoft.EntityFrameworkCore;
using Techies.Web.Domain;
using Techies.Web.Services;
using Techies.Web.ViewModels.Account;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class CustomerAccountTests(PostgresFixture fixture)
{
    private static RegisterForm Form(string email = "lan@example.com") => new()
    {
        FullName = "Nguyễn Thị Lan", Email = email, Phone = "0912345678",
        Password = "Matkhau@1", ConfirmPassword = "Matkhau@1",
    };

    private async Task<CustomerAccountService> ServiceAsync(Web.Data.AppDbContext db)
    {
        await new Web.Data.Seed.DbSeeder(db, TimeProvider.System).SeedAsync(); // tiers
        return new CustomerAccountService(db, TimeProvider.System);
    }

    [Fact]
    public async Task Register_creates_customer_with_tier_0_loyalty()
    {
        await using var db = await fixture.CreateContextAsync();
        var service = await ServiceAsync(db);

        var user = await service.RegisterAsync(Form("Lan@Example.com"));

        var stored = await db.Users.Include(u => u.Loyalty).SingleAsync(u => u.Id == user.Id);
        Assert.Equal("lan@example.com", stored.Email);
        Assert.Equal(0, stored.Loyalty!.Tier);
        Assert.Equal(0, stored.Loyalty.Balance);
    }

    [Fact]
    public async Task Register_rejects_email_already_used_in_other_case()
    {
        await using var db = await fixture.CreateContextAsync();
        var service = await ServiceAsync(db);
        await service.RegisterAsync(Form("lan@example.com"));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.RegisterAsync(Form("LAN@example.com")));
        Assert.Equal("Email đã được sử dụng.", ex.Message);
    }

    [Fact]
    public async Task Authenticate_refuses_locked_customer()
    {
        await using var db = await fixture.CreateContextAsync();
        var service = await ServiceAsync(db);
        var user = await service.RegisterAsync(Form());
        user.Status = AccountStatus.Locked;
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.AuthenticateAsync("lan@example.com", "Matkhau@1"));
        Assert.Equal("Tài khoản đã bị khóa.", ex.Message);
    }

    [Fact]
    public async Task Authenticate_rejects_wrong_password()
    {
        await using var db = await fixture.CreateContextAsync();
        var service = await ServiceAsync(db);
        await service.RegisterAsync(Form());

        await Assert.ThrowsAsync<DomainException>(() => service.AuthenticateAsync("lan@example.com", "sai"));
    }
}

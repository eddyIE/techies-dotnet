using Techies.Web.Domain;
using Techies.Web.Services;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class CustomerQueryTests(PostgresFixture fixture)
{
    [Theory]
    [InlineData("lan.nguyen@")]
    [InlineData("thị lan")]
    [InlineData("0912345")]
    public async Task Search_matches_email_name_or_phone(string q)
    {
        await using var db = await fixture.CreateContextAsync();
        TestData.Tiers(db);
        var lan = TestData.Customer(db, "lan.nguyen@example.com");
        lan.FullName = "Nguyễn Thị Lan";
        lan.Phone = "0912345678";
        TestData.Customer(db, "minh@example.com");
        await db.SaveChangesAsync();

        var page = await new CustomerQueries(db).SearchAsync(q, page: 1);

        Assert.Equal([lan.Id], page.Items.Select(c => c.Id));
    }

    [Fact]
    public async Task Locking_a_customer_blocks_login_and_unlocking_restores_it()
    {
        await using var db = await fixture.CreateContextAsync();
        TestData.Tiers(db);
        var accounts = new CustomerAccountService(db, TimeProvider.System);
        var user = await accounts.RegisterAsync(new() { FullName = "Lan", Email = "lan@example.com", Phone = "0912345678", Password = "Matkhau@1", ConfirmPassword = "Matkhau@1" });
        var queries = new CustomerQueries(db);

        Assert.Equal(AccountStatus.Locked, await queries.ToggleLockAsync(user.Id));
        await Assert.ThrowsAsync<DomainException>(() => accounts.AuthenticateAsync("lan@example.com", "Matkhau@1"));

        Assert.Equal(AccountStatus.Active, await queries.ToggleLockAsync(user.Id));
        Assert.Equal(user.Id, (await accounts.AuthenticateAsync("lan@example.com", "Matkhau@1")).Id);
    }
}

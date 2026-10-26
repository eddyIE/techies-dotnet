using Microsoft.AspNetCore.Http;
using Techies.Web.Domain;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.Services;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class CustomerQueryTests(PostgresFixture fixture)
{
    private static GridRequest Request(string query) =>
        GridRequest.From("/admin/customers", new QueryString(query), CustomerQueries.GridSorts, "name", false);

    [Theory]
    [InlineData("lan.nguyen@")]
    [InlineData("thị lan")]
    [InlineData("0912345")]
    public async Task Keyword_matches_email_name_or_phone(string q)
    {
        await using var db = await fixture.CreateContextAsync();
        TestData.Tiers(db);
        var lan = TestData.Customer(db, "lan.nguyen@example.com");
        lan.FullName = "Nguyễn Thị Lan";
        lan.Phone = "0912345678";
        TestData.Customer(db, "minh@example.com");
        await db.SaveChangesAsync();

        var (rows, _) = await new CustomerQueries(db, TimeProvider.System).GridAsync(Request("?q=" + Uri.EscapeDataString(q)));

        Assert.Equal([lan.Id], rows.Select(c => c.Id));
    }

    [Fact]
    public async Task Tier_and_status_filters_with_balance_sort()
    {
        await using var db = await fixture.CreateContextAsync();
        TestData.Tiers(db);
        var gold = TestData.Customer(db, "gold@example.com", lifetimePoints: 40_000);
        gold.Loyalty!.Tier = 2;
        var goldLocked = TestData.Customer(db, "gold2@example.com", lifetimePoints: 35_000);
        goldLocked.Loyalty!.Tier = 2;
        goldLocked.Status = AccountStatus.Locked;
        var gold3 = TestData.Customer(db, "gold3@example.com", lifetimePoints: 50_000);
        gold3.Loyalty!.Tier = 2;
        TestData.Customer(db, "basic@example.com");
        await db.SaveChangesAsync();

        var (rows, total) = await new CustomerQueries(db, TimeProvider.System).GridAsync(Request("?tier=2&status=Active&sort=balance&dir=desc"));

        Assert.Equal(2, total);
        Assert.Equal([gold3.Id, gold.Id], rows.Select(c => c.Id));
    }

    [Fact]
    public async Task Locking_a_customer_blocks_login_and_unlocking_restores_it()
    {
        await using var db = await fixture.CreateContextAsync();
        TestData.Tiers(db);
        var accounts = new CustomerAccountService(db, TimeProvider.System);
        var user = await accounts.RegisterAsync(new() { FullName = "Lan", Email = "lan@example.com", Phone = "0912345678", Password = "Matkhau@1", ConfirmPassword = "Matkhau@1" });
        var queries = new CustomerQueries(db, TimeProvider.System);

        await queries.SetStatusAsync(user.Id, AccountStatus.Locked);
        await Assert.ThrowsAsync<DomainException>(() => accounts.AuthenticateAsync("lan@example.com", "Matkhau@1"));

        await queries.SetStatusAsync(user.Id, AccountStatus.Active);
        Assert.Equal(user.Id, (await accounts.AuthenticateAsync("lan@example.com", "Matkhau@1")).Id);
    }
}

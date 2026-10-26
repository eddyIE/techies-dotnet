using Microsoft.EntityFrameworkCore;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class DatabaseFixtureTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Fresh_database_has_migrations_applied()
    {
        await using var db = await fixture.CreateContextAsync();

        var applied = await db.Database.GetAppliedMigrationsAsync();

        Assert.Contains(applied, m => m.EndsWith("_Initial"));
    }
}

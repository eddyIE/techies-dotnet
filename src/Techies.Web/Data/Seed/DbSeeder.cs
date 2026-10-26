namespace Techies.Web.Data.Seed;

// Runs on every startup; each step skips itself when its data already exists.
public sealed class DbSeeder(AppDbContext db)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        await Task.CompletedTask;
    }
}

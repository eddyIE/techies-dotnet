using Microsoft.EntityFrameworkCore;
using Techies.Web.Data.Seed;

namespace Techies.Web.Data;

public static class DatabaseSetup
{
    public static void AddAppDatabase(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing connection string 'Default'.");
        services.AddDbContext<AppDbContext>(options => Configure(options, connectionString));
        services.AddScoped<DbSeeder>();
    }

    public static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string connectionString) =>
        options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();

    public static async Task MigrateAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
    }
}

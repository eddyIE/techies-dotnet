using Microsoft.EntityFrameworkCore;
using Npgsql;
using Techies.Web.Data;
using Testcontainers.PostgreSql;

namespace Techies.Tests;

// One container for the whole test run. Migrations are applied once to a template database;
// every test then gets its own copy (CREATE DATABASE ... TEMPLATE), so tests never share rows.
public sealed class PostgresFixture : IAsyncLifetime
{
    private const string TemplateDb = "techies_template";
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private int _counter;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await ExecuteAsync($"CREATE DATABASE {TemplateDb}");
        await using var db = new AppDbContext(Options(TemplateDb));
        await db.Database.MigrateAsync();
        // A pooled connection left open on the template would block CREATE DATABASE ... TEMPLATE.
        NpgsqlConnection.ClearAllPools();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    // A fresh, migrated, empty database for one test.
    public async Task<AppDbContext> CreateContextAsync()
    {
        var name = $"test_{Interlocked.Increment(ref _counter)}";
        await ExecuteAsync($"CREATE DATABASE {name} TEMPLATE {TemplateDb}");
        return new AppDbContext(Options(name));
    }

    // A second context on the same database, e.g. to simulate a concurrent request.
    public static AppDbContext CreateSibling(AppDbContext db) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(db.Database.GetConnectionString()).UseSnakeCaseNamingConvention().Options);

    private DbContextOptions<AppDbContext> Options(string database)
    {
        // No pooling: every test has its own database, and one idle pool per database would
        // exhaust the server's connection limit as the suite grows.
        var cs = new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = database, Pooling = false };
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        DatabaseSetup.Configure(builder, cs.ConnectionString);
        return builder.Options;
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var conn = new NpgsqlConnection(_container.GetConnectionString());
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}

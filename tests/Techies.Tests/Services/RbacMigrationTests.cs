using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Techies.Web.Domain;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class RbacMigrationTests(PostgresFixture fixture)
{
    private static string Name(string suffix, IMigrationsAssembly migrations) =>
        migrations.Migrations.Keys.Single(k => k.EndsWith("_" + suffix));

    [Fact]
    public async Task Existing_staff_are_mapped_from_the_old_role_column()
    {
        await using var db = await fixture.CreateEmptyContextAsync();
        var migrations = db.GetService<IMigrationsAssembly>();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(Name("Reviews", migrations)); // the step before Rbac
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO staff (id, full_name, email, password_hash, role, status, created_at, updated_at) VALUES
              (gen_random_uuid(), 'A', 'a@x.vn', 'x', 'SUPER_ADMIN', 'ACTIVE', now(), now()),
              (gen_random_uuid(), 'K', 'k@x.vn', 'x', 'CATALOG_STAFF', 'ACTIVE', now(), now()),
              (gen_random_uuid(), 'Q', 'q@x.vn', 'x', 'ORDER_STAFF', 'LOCKED', now(), now());
            """);

        await migrator.MigrateAsync();

        var roles = await db.Staff.AsNoTracking().OrderBy(s => s.Email).Select(s => s.RoleId).ToListAsync();
        Assert.Equal([SeededRoles.SuperAdmin, SeededRoles.CatalogStaff, SeededRoles.OrderStaff], roles);
        // A role created afterwards must not collide with the seeded ids.
        db.Roles.Add(new Role { Code = "NEW", Name = "Mới" });
        await db.SaveChangesAsync();
        Assert.True(await db.Roles.MaxAsync(r => r.Id) > SeededRoles.OrderStaff);
    }

    [Fact]
    public async Task Unknown_old_role_value_fails_the_migration()
    {
        await using var db = await fixture.CreateEmptyContextAsync();
        var migrations = db.GetService<IMigrationsAssembly>();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(Name("Reviews", migrations));
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO staff (id, full_name, email, password_hash, role, status, created_at, updated_at)
            VALUES (gen_random_uuid(), 'X', 'x@x.vn', 'x', 'JANITOR', 'ACTIVE', now(), now());
            """);

        await Assert.ThrowsAsync<PostgresException>(() => migrator.MigrateAsync());
    }
}

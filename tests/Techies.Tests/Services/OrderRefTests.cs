using Microsoft.EntityFrameworkCore;
using Techies.Web.Data.Seed;
using Techies.Web.Domain;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class OrderRefTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Orders_get_consecutive_refs_from_the_sequence()
    {
        await using var db = await fixture.CreateContextAsync();
        await new DbSeeder(db, TimeProvider.System).SeedAsync();
        var customer = await db.Users.FirstAsync();
        Order Make() => new()
        {
            UserId = customer.Id, Status = OrderStatus.Confirmed, PaymentMethod = PaymentMethod.Cod,
            PaymentStatus = PaymentStatus.Pending, ShipRecipient = "A", ShipPhone = "0900000000", ShipLine1 = "1",
            ShipWard = "W", ShipDistrict = "D", ShipProvince = "P", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow,
        };
        var first = Make();
        var second = Make();
        db.Orders.Add(first);
        await db.SaveChangesAsync();
        db.Orders.Add(second);
        await db.SaveChangesAsync();

        Assert.Matches(@"^ORD-\d{6}$", first.OrderRef);
        Assert.Equal(long.Parse(first.OrderRef[4..]) + 1, long.Parse(second.OrderRef[4..]));
    }
}

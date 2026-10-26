using Microsoft.EntityFrameworkCore;
using Techies.Web.Domain;
using Techies.Web.Services;

namespace Techies.Web.Data.Seed;

// Generated order history for the demo, anchored on the seeding time so the dashboard always
// has recent data. A fixed Random seed keeps it reproducible; nothing is dated in the future.
//
// - One COMPLETED order per ported review (reviews.json, from ../techies order-service V7/V12),
//   so every review belongs to a real purchase of that product.
// - Extra orders covering every status, payment method/status and failure code.
// - Coupon redemptions, EARN point transactions and tiers consistent with COMPLETED orders.
// - Gift redemptions (ISSUED and CLAIMED) paid from the points earned.
//
// Seeded orders do not move stock: the ported stock levels are taken as today's levels.
internal static class OrderSeed
{
    private const int ExtraOrders = 90;
    private const int HistoryDays = 450;
    private static readonly string[] FailureCodes = ["PAYMENT_FAILED", "OUT_OF_STOCK", "SERVICE_UNAVAILABLE"];
    private static readonly string[] SeedCoupons = ["TECHIES50K", "TECHIES500K", "FREESHIP30K"];

    private sealed record ReviewRow(Guid Id, Guid ProductId, short Rating, string? Comment);

    public static async Task SeedAsync(AppDbContext db, DateTimeOffset now, CancellationToken ct)
    {
        if (await db.Orders.AnyAsync(ct)) return;

        var random = new Random(4242);
        var today = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        var customers = await db.Users.Include(u => u.Addresses).OrderBy(u => u.Email).ToListAsync(ct);
        var products = await db.Products.Where(p => p.Active).OrderBy(p => p.Name).ToListAsync(ct);
        var productById = products.ToDictionary(p => p.Id);
        var coupons = await db.Coupons.Where(c => SeedCoupons.Contains(c.Code)).ToListAsync(ct);
        var tiers = await db.MembershipTiers.ToListAsync(ct);
        var counterStaff = await db.Staff.Where(s => s.Role == StaffRole.OrderStaff).Select(s => s.Id).FirstAsync(ct);

        // Skewed so a few customers buy often (high tiers) and most buy rarely.
        var weights = customers.Select((_, i) => 1.0 / (i + 1)).ToArray();
        User PickCustomer() => customers[WeightedIndex(random, weights)];

        var plans = new List<(Order Order, ReviewRow? Review)>();
        foreach (var review in SeedFiles.Read<ReviewRow>("reviews.json").Where(r => productById.ContainsKey(r.ProductId)))
        {
            var created = today.AddDays(-random.Next(8, HistoryDays)).AddMinutes(random.Next(8 * 60, 22 * 60));
            var order = NewOrder(random, PickCustomer(), created, [productById[review.ProductId], .. MaybeExtraProduct(random, products)]);
            Finish(order, OrderStatus.Completed, random, PaymentMethodFor(random));
            plans.Add((order, review));
        }
        for (var i = 0; i < ExtraOrders; i++)
        {
            var ageDays = i < 25 ? random.Next(0, 6) : random.Next(6, HistoryDays);
            var created = Min(today.AddDays(-ageDays).AddMinutes(random.Next(8 * 60, 22 * 60)), now.AddMinutes(-30 - i));
            var order = NewOrder(random, PickCustomer(), created, Products(random, products));
            var method = PaymentMethodFor(random);
            var status = ageDays < 6
                ? Pick(random, [(OrderStatus.Confirmed, 60), (OrderStatus.AwaitingPayment, 25), (OrderStatus.Failed, 15)])
                : Pick(random, [(OrderStatus.Completed, 70), (OrderStatus.Cancelled, 18), (OrderStatus.Failed, 12)]);
            if (status == OrderStatus.AwaitingPayment) method = PaymentMethod.MockCard; // only card payments wait
            Finish(order, status, random, method);
            plans.Add((order, null));
        }

        // Insert oldest first so order refs (ORD-100001, ...) ascend with time.
        plans.Sort((a, b) => a.Order.CreatedAt.CompareTo(b.Order.CreatedAt));
        foreach (var (order, _) in plans)
        {
            if (order.Status != OrderStatus.Failed && random.NextDouble() < 0.2)
                ApplyCoupon(db, order, Pick(random, coupons), order.CreatedAt);
            db.Orders.Add(order);
        }
        await db.SaveChangesAsync(ct);

        var loyalty = await db.CustomerLoyalty.ToDictionaryAsync(l => l.CustomerId, ct);
        foreach (var (order, review) in plans)
        {
            if (order.Status != OrderStatus.Completed) continue;
            var points = OrderMath.PointsFor(order.Subtotal, order.Discount);
            var account = loyalty[order.UserId];
            account.LifetimePoints += points;
            account.Balance += points;
            account.UpdatedAt = order.CompletedAt!.Value;
            db.PointTransactions.Add(new PointTransaction
            {
                CustomerId = order.UserId, TransactionType = PointTransactionType.Earn, PointsDelta = points,
                OrderId = order.Id, Note = $"Hoàn tất đơn {order.OrderRef}", CreatedAt = order.CompletedAt.Value,
            });
            if (review is not null)
                db.Reviews.Add(new Review
                {
                    Id = review.Id, CustomerId = order.UserId, ProductId = review.ProductId, OrderId = order.Id,
                    Rating = review.Rating, Comment = review.Comment,
                    CreatedAt = Min(order.CompletedAt.Value.AddDays(random.Next(1, 10)), now),
                });
        }
        foreach (var account in loyalty.Values)
            account.Tier = LoyaltyService.TierFor(account.LifetimePoints, tiers);

        await SeedGiftRedemptionsAsync(db, random, customers, loyalty, tiers, counterStaff, today, now, ct);
        await db.SaveChangesAsync(ct);
    }

    private static Order NewOrder(Random random, User customer, DateTimeOffset created, IEnumerable<Product> products)
    {
        var address = customer.Addresses.OrderByDescending(a => a.IsDefault).First();
        var items = products.DistinctBy(p => p.Id).Select(p =>
        {
            var quantity = p.Price < 2_000_000 ? random.Next(1, 4) : 1;
            return new OrderItem
            {
                ProductId = p.Id, ProductName = p.Name, UnitPrice = p.Price, ThumbnailUrl = p.ThumbnailUrl,
                Quantity = quantity, LineTotal = p.Price * quantity,
            };
        }).ToList();
        var subtotal = items.Sum(i => i.LineTotal);
        var shipping = OrderMath.ShippingFee(subtotal);
        return new Order
        {
            UserId = customer.Id, Items = items, Subtotal = subtotal, ShippingFee = shipping,
            Total = OrderMath.Total(subtotal, shipping, 0),
            ShipRecipient = address.RecipientName, ShipPhone = address.Phone, ShipLine1 = address.Line1,
            ShipWard = address.Ward, ShipDistrict = address.District, ShipProvince = address.Province,
            CreatedAt = created, UpdatedAt = created,
        };
    }

    // Status, payment and timestamps that agree with each other (BRD §3).
    private static void Finish(Order order, OrderStatus status, Random random, PaymentMethod method)
    {
        order.Status = status;
        order.PaymentMethod = method;
        var card = method == PaymentMethod.MockCard;
        switch (status)
        {
            case OrderStatus.AwaitingPayment:
                order.PaymentStatus = PaymentStatus.Pending;
                break;
            case OrderStatus.Confirmed:
                order.PaymentStatus = card ? PaymentStatus.Paid : PaymentStatus.Pending;
                break;
            case OrderStatus.Completed:
                order.PaymentStatus = PaymentStatus.Paid;
                order.CompletedAt = order.CreatedAt.AddDays(random.Next(1, 5)).AddHours(random.Next(1, 8));
                order.UpdatedAt = order.CompletedAt.Value;
                break;
            case OrderStatus.Cancelled:
                order.PaymentStatus = card ? PaymentStatus.Refunded : PaymentStatus.Pending;
                order.UpdatedAt = order.CreatedAt.AddDays(random.Next(1, 3));
                break;
            case OrderStatus.Failed:
                order.FailureCode = card ? Pick(random, FailureCodes) : Pick(random, FailureCodes[1..]);
                order.PaymentStatus = order.FailureCode == "PAYMENT_FAILED" ? PaymentStatus.Declined : PaymentStatus.Pending;
                break;
        }
    }

    private static void ApplyCoupon(AppDbContext db, Order order, Coupon coupon, DateTimeOffset at)
    {
        var evaluation = CouponService.Evaluate(coupon, order.Subtotal, at);
        if (!evaluation.Applicable) return;
        order.CouponCode = coupon.Code;
        order.Discount = evaluation.Discount;
        order.Total = OrderMath.Total(order.Subtotal, order.ShippingFee, order.Discount);
        var cancelled = order.Status == OrderStatus.Cancelled;
        db.CouponRedemptions.Add(new CouponRedemption
        {
            CouponId = coupon.Id, OrderId = order.Id, CustomerId = order.UserId, DiscountAmount = order.Discount,
            Status = cancelled ? CouponRedemptionStatus.Restored : CouponRedemptionStatus.Applied,
            RedeemedAt = at, RestoredAt = cancelled ? order.UpdatedAt : null,
        });
    }

    private static async Task SeedGiftRedemptionsAsync(AppDbContext db, Random random, List<User> customers,
        Dictionary<Guid, CustomerLoyalty> loyalty, List<MembershipTier> tiers, Guid counterStaff,
        DateTimeOffset today, DateTimeOffset now, CancellationToken ct)
    {
        var gifts = await db.Gifts.Where(g => g.Active && g.StockAvailable > 0).OrderBy(g => g.PointsCost).ToListAsync(ct);
        var count = 0;
        foreach (var customer in customers)
        {
            var account = loyalty[customer.Id];
            var affordable = gifts.Where(g => g.PointsCost <= account.Balance && g.MinTier <= account.Tier).ToList();
            if (affordable.Count == 0 || random.NextDouble() < 0.4) continue;

            var gift = Pick(random, affordable);
            var issued = count < 6; // the first few are still waiting to be collected
            var redeemedAt = issued ? today.AddDays(-random.Next(0, 5)).AddHours(10) : today.AddDays(-random.Next(10, 200)).AddHours(10);
            var redemption = new GiftRedemption
            {
                // The BRD's example code, so the counter screen can be demoed straight from the document.
                Code = count == 0 ? "GIFT-G53Y-ZDS3" : NewGiftCode(random),
                GiftId = gift.Id, CustomerId = customer.Id, PointsSpent = gift.PointsCost, RedeemedAt = Min(redeemedAt, now),
                Status = issued ? GiftRedemptionStatus.Issued : GiftRedemptionStatus.Claimed,
                ClaimedAt = issued ? null : redeemedAt.AddDays(random.Next(1, 7)),
                ClaimedBy = issued ? null : counterStaff,
            };
            db.GiftRedemptions.Add(redemption);
            db.PointTransactions.Add(new PointTransaction
            {
                CustomerId = customer.Id, TransactionType = PointTransactionType.Redeem, PointsDelta = -gift.PointsCost,
                GiftRedemptionId = redemption.Id, Note = $"Đổi quà {gift.Name}", CreatedAt = redemption.RedeemedAt,
            });
            account.Balance -= gift.PointsCost;
            gift.StockAvailable--;
            count++;
        }
    }

    private static string NewGiftCode(Random random)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        string Part() => new(Enumerable.Range(0, 4).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        return $"GIFT-{Part()}-{Part()}";
    }

    private static PaymentMethod PaymentMethodFor(Random random) =>
        random.NextDouble() < 0.65 ? PaymentMethod.Cod : PaymentMethod.MockCard;

    private static IEnumerable<Product> MaybeExtraProduct(Random random, List<Product> products) =>
        random.NextDouble() < 0.25 ? [Pick(random, products.Where(p => p.Price < 2_000_000).ToList())] : [];

    private static IEnumerable<Product> Products(Random random, List<Product> products) =>
        Enumerable.Range(0, random.Next(1, 4)).Select(_ => Pick(random, products));

    private static T Pick<T>(Random random, IReadOnlyList<T> items) => items[random.Next(items.Count)];

    private static T Pick<T>(Random random, (T Value, int Weight)[] options)
    {
        var roll = random.Next(options.Sum(o => o.Weight));
        foreach (var (value, weight) in options)
        {
            if (roll < weight) return value;
            roll -= weight;
        }
        return options[^1].Value;
    }

    private static int WeightedIndex(Random random, double[] weights)
    {
        var roll = random.NextDouble() * weights.Sum();
        for (var i = 0; i < weights.Length; i++)
        {
            if (roll < weights[i]) return i;
            roll -= weights[i];
        }
        return weights.Length - 1;
    }

    private static DateTimeOffset Min(DateTimeOffset a, DateTimeOffset b) => a < b ? a : b;
}

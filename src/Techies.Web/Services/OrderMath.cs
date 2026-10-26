using Techies.Web.Domain;

namespace Techies.Web.Services;

// Pure order arithmetic, shared by checkout, admin screens and seeding.
public static class OrderMath
{
    public const decimal FlatShippingFee = 30_000;
    public const decimal FreeShippingFrom = 500_000;

    // Same policy as ../techies ShippingPolicy: flat fee, waived from the threshold.
    public static decimal ShippingFee(decimal subtotal) => subtotal >= FreeShippingFrom ? 0 : FlatShippingFee;

    public static decimal Total(decimal subtotal, decimal shippingFee, decimal discount) => subtotal + shippingFee - discount;

    // BRD §3 "Số tiền cần thu hộ": the full total for unpaid COD, nothing once paid.
    public static decimal AmountToCollect(Order order) =>
        order.PaymentMethod == PaymentMethod.Cod && order.PaymentStatus != PaymentStatus.Paid ? order.Total : 0;

    // BRD §3: 1 point for every 1.000đ of (subtotal - discount).
    public static long PointsFor(decimal subtotal, decimal discount) => (long)Math.Floor((subtotal - discount) / 1000m);
}

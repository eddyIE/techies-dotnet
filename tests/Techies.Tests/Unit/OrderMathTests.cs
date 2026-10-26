using Techies.Web.Domain;
using Techies.Web.Services;

namespace Techies.Tests.Unit;

public class OrderMathTests
{
    [Theory]
    [InlineData(499_999, 30_000)]
    [InlineData(500_000, 0)]
    [InlineData(0, 30_000)]
    public void Shipping_is_flat_30k_waived_from_500k(decimal subtotal, decimal expected) =>
        Assert.Equal(expected, OrderMath.ShippingFee(subtotal));

    [Fact]
    public void Total_is_subtotal_plus_shipping_minus_discount() =>
        Assert.Equal(1_230_000, OrderMath.Total(subtotal: 1_250_000, shippingFee: 30_000, discount: 50_000));

    [Fact]
    public void Cod_pending_order_collects_full_total()
    {
        var order = new Order { PaymentMethod = PaymentMethod.Cod, PaymentStatus = PaymentStatus.Pending, Total = 1_230_000 };

        Assert.Equal(1_230_000, OrderMath.AmountToCollect(order));
    }

    [Fact]
    public void Paid_card_order_collects_nothing()
    {
        var order = new Order { PaymentMethod = PaymentMethod.MockCard, PaymentStatus = PaymentStatus.Paid, Total = 1_230_000 };

        Assert.Equal(0, OrderMath.AmountToCollect(order));
    }

    [Theory]
    [InlineData(1_250_000, 50_000, 1_200)]
    [InlineData(1_999, 0, 1)]
    [InlineData(999, 0, 0)]
    public void Points_are_one_per_1000_of_subtotal_minus_discount(decimal subtotal, decimal discount, long expected) =>
        Assert.Equal(expected, OrderMath.PointsFor(subtotal, discount));
}

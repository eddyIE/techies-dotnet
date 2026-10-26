using Techies.Web.Domain;
using Techies.Web.Services;

namespace Techies.Tests.Unit;

public class CartTests
{
    private static readonly Guid Phone = Guid.NewGuid();

    [Fact]
    public void Adding_same_product_twice_merges_quantity()
    {
        var cart = new Cart();

        cart.Add(Phone, 1, available: 10);
        cart.Add(Phone, 2, available: 10);

        Assert.Equal(3, cart.Lines[Phone]);
    }

    [Fact]
    public void Quantity_is_capped_at_available_stock()
    {
        var cart = new Cart();

        cart.Add(Phone, 5, available: 3);

        Assert.Equal(3, cart.Lines[Phone]);
    }

    [Fact]
    public void Quantity_is_capped_at_99()
    {
        var cart = new Cart();

        cart.Add(Phone, 150, available: 500);

        Assert.Equal(99, cart.Lines[Phone]);
    }

    [Fact]
    public void Adding_out_of_stock_product_is_rejected() =>
        Assert.Throws<DomainException>(() => new Cart().Add(Phone, 1, available: 0));

    [Fact]
    public void Setting_quantity_to_zero_removes_line()
    {
        var cart = new Cart();
        cart.Add(Phone, 2, available: 10);

        cart.Set(Phone, 0, available: 10);

        Assert.Empty(cart.Lines);
    }

    [Fact]
    public void Round_trips_through_json()
    {
        var cart = new Cart();
        cart.Add(Phone, 2, available: 10);

        Assert.Equal(2, Cart.FromJson(cart.ToJson()).Lines[Phone]);
    }
}

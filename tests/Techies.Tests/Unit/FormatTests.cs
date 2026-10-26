using Techies.Web.Infrastructure;

namespace Techies.Tests.Unit;

public class FormatTests
{
    [Theory]
    [InlineData(1234000, "1.234.000đ")]
    [InlineData(0, "0đ")]
    [InlineData(30000.50, "30.001đ")]
    public void Money_uses_vietnamese_grouping_and_dong_sign(decimal amount, string expected) =>
        Assert.Equal(expected, Format.Money(amount));

    [Fact]
    public void LocalTime_converts_utc_to_ho_chi_minh_time()
    {
        var utc = new DateTimeOffset(2026, 10, 26, 7, 5, 0, TimeSpan.Zero);

        Assert.Equal("26/10/2026 14:05", Format.LocalTime(utc));
    }

    [Fact]
    public void LocalTime_handles_crossing_midnight()
    {
        var utc = new DateTimeOffset(2026, 12, 31, 18, 30, 0, TimeSpan.Zero);

        Assert.Equal("01/01/2027 01:30", Format.LocalTime(utc));
    }
}
